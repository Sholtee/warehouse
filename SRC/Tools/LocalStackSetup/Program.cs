/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.ResourceGroupsTaggingAPI;
using Amazon.ResourceGroupsTaggingAPI.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

using static System.Environment;

namespace Warehouse.Tools.LocalStackSetup
{
    internal static class Program
    {
        #region Private
        private const string
            TAG_KEY = "Project",
            TAG_VALUE = "local-warehouse-app";

        #pragma warning disable CA1812  // this class is instantiated by the JsonSerializer
        private sealed class LocalStackStatus
        #pragma warning restore CA1812 
        {
            public required Dictionary<string, string> Services { get; init; }
            public required string Edition { get; init; }
            public required string Version { get; init; }
        }

        private static async Task WiatForServices(params string[] services)
        {
            //
            // Wait for LocalStack to be ready
            //

            using HttpClient httpClient = new();

            httpClient.BaseAddress = new Uri(GetEnvironmentVariable("AWS_ENDPOINT_URL")!);

            JsonSerializerOptions serializerOpts = new()
            {
                PropertyNameCaseInsensitive = true
            };

            for (int i = 0; i < int.Parse(GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "10", null); i++)
            {
                await Task.Delay(2000 * i);

                try
                {
                    HttpResponseMessage resp = await httpClient.GetAsync("/_localstack/health");
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        Console.WriteLine($"LocalStack responded bad status: {resp.StatusCode}");
                        continue;
                    }

                    LocalStackStatus? status = await resp.Content.ReadFromJsonAsync<LocalStackStatus>(serializerOpts);
                    foreach (string service in services)
                    {
                        if (status?.Services.TryGetValue(service, out string? serviceStatus) is not true || serviceStatus is not "available")
                        {
                            Console.WriteLine($"Service not available: {service}");
                            goto again;
                        }
                    }

                    Console.WriteLine("LocalStack is READY.");
                    return;

                    again:;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Connection failed to LocalStack: {ex.Message}");
                    continue;
                }
            }

            throw new InvalidOperationException("Failed to setup LocalStack");
        }

        private static async Task SetupSecrets()
        {
            using AmazonSecretsManagerClient client = new();

            await SetupSecret("local-jwt-secret-key", GetEnvironmentVariable("JWT_SECRET")!);

            await SetupSecret("local-db-secret", GetEnvironmentVariable("DB_SECRET")!);

            async Task SetupSecret(string name, object value)
            {
                Console.WriteLine($"Setup '{name}'...");

                if (value is not string valueStr)
                    valueStr = JsonSerializer.Serialize(value);

                await client.CreateSecretAsync
                (
                    new CreateSecretRequest
                    {
                        Name = name,
                        SecretString = valueStr,
                        Tags =
                        [
                            new Amazon.SecretsManager.Model.Tag
                            {
                                Key = TAG_KEY,
                                Value = TAG_VALUE
                            }
                        ]
                    }
                );
            }
        }

        private static async Task SetupCertificate()
        {
            using AmazonCertificateManagerClient client = new();

            ImportCertificateResponse resp = await client.ImportCertificateAsync
            (
                new ImportCertificateRequest
                {
                    Certificate = await ReadAsStream(Path.Combine("Cert", "client.crt")),
                    PrivateKey = await ReadAsStream(Path.Combine("Cert", "client.key")),
                    Tags =
                    [
                        new Amazon.CertificateManager.Model.Tag
                        {
                            Key = TAG_KEY,
                            Value = TAG_VALUE
                        }
                    ]
                }
            );

            Console.WriteLine($"Created certificate: {resp.CertificateArn}");

            static async Task<MemoryStream> ReadAsStream(string file) => new MemoryStream
            (
                Encoding.UTF8.GetBytes
                (
                    await File.ReadAllTextAsync(file)
                )
            );
        }

        private static async Task<bool> HasAppResources()
        {
            using AmazonResourceGroupsTaggingAPIClient client = new();

            GetResourcesResponse resp = await client.GetResourcesAsync
            (
                new GetResourcesRequest
                {
                    TagFilters =
                    [
                        new TagFilter
                        {
                            Key = TAG_KEY,
                            Values = [TAG_VALUE]
                        }
                    ]
                }
            );

            string[] appResources = [.. resp.ResourceTagMappingList.Select(static tm => tm.ResourceARN)];
            Console.WriteLine($"Initialized LocalStack resources: {(appResources.Length > 0 ? string.Join(", ", appResources) : "-")}");

            return appResources.Length > 0;
        }
        #endregion

        public static async Task Main()
        {
            await WiatForServices("secretsmanager", "acm");

            if (await HasAppResources())
            {
                Console.WriteLine("LocalStack alread initialized, terminating...");
                return;
            }

            await SetupCertificate();
            await SetupSecrets();

            Console.WriteLine("LocalStack initialized successfully :)");
        }
    }
}
