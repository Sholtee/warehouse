/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Lambda;
using Amazon.Lambda.Model;
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
            TAG_VALUE = "local-warehouse-app",
            MIGRATOR_NAME = "local-warehouse-db-migrator-lambda";

        #pragma warning disable CA1812  // this class is instantiated by the JsonSerializer
        private sealed class LocalStackStatus
        #pragma warning restore CA1812 
        {
            public required Dictionary<string, string> Services { get; init; }
            public required string Edition { get; init; }
            public required string Version { get; init; }
        }

        private static async Task WaitForServices(params string[] services)
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

            await SetupSecret("local-warehouse-jwt-secret-key", GetEnvironmentVariable("JWT_SECRET")!);

            await SetupSecret("local-warehouse-db-secret", GetEnvironmentVariable("DB_SECRET")!);

            await SetupSecret("local-warehouse-app-cert", new
            {
                privateKey = await File.ReadAllTextAsync(Path.Combine("Cert", "client.key")),
                certificate = await File.ReadAllTextAsync(Path.Combine("Cert", "client.crt"))
            });

            await SetupSecret("local-warehouse-root-user-password", GetEnvironmentVariable("ROOT_PASSWORD")!);

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

        private static async Task SetupDbMigratorLambda()
        {
            string[] variablesToCopy = ["AWS_ENDPOINT_URL", "AWS_REGION", "AWS_ACCESS_KEY_ID", "AWS_SECRET_ACCESS_KEY"];

            IDictionary vars = GetEnvironmentVariables();

            Console.WriteLine("Loading DB Migrator lambda binaries");

            using MemoryStream zipFile = new();
            using (FileStream lambdaBinaries = File.OpenRead(Path.Combine("LambdaBinaries", "DbMigrator.zip")))
            {
                await lambdaBinaries.CopyToAsync(zipFile);
                zipFile.Seek(0, SeekOrigin.Begin);
            }

            Console.WriteLine("Creating DB Migrator lambda");

            using AmazonLambdaClient client = new();

            await client.CreateFunctionAsync
            (
                new CreateFunctionRequest
                {
                    FunctionName = MIGRATOR_NAME,
                    Runtime = Runtime.Dotnet8,
                    Handler = "Warehouse.Tools.DbMigrator::DbMigrator.LambdaFunction::Handler",
                    Role = "arn:aws:iam::000000000000:role/lambda-role",
                    Timeout = 60,
                    MemorySize = 128,
                    Environment = new()
                    {
                        Variables = new Dictionary<string, string>
                        (
                            vars
                                .Keys
                                .Cast<string>()
                                .Where(variablesToCopy.Contains)
                                .ToDictionary(static key => key, key => (string) vars[key]!)
                        )
                        {
                            { "PREFIX", "local" }
                        }
                    },
                    Code = new FunctionCode
                    {
                        ZipFile = zipFile
                    },
                    Tags = new Dictionary<string, string>
                    {
                        { TAG_KEY, TAG_VALUE }
                    }
                }
            );

            Console.WriteLine("Wait for lambda to be activated");

            GetFunctionConfigurationResponse resp;

            while ((resp = await client.GetFunctionConfigurationAsync(MIGRATOR_NAME)).State == State.Pending)
            {
                await Task.Delay(1000);
            }

            if (resp.State != State.Active)
                throw new InvalidOperationException($"Lambda got bad state {resp.State}");

            Console.WriteLine("DB Migrator lambda created successfully");
        }

        private static async Task InitDb()
        {
            Console.WriteLine("Init DataBase"); ;

            using AmazonLambdaClient client = new();

            InvokeResponse resp = await client.InvokeAsync(new InvokeRequest
            {
                FunctionName = MIGRATOR_NAME,
            });

            if (!string.IsNullOrEmpty(resp.FunctionError))
            {
                #pragma warning disable CA2000 // "sr" is disposed properly
                using StreamReader sr = new(resp.Payload, leaveOpen: false);
                #pragma warning restore CA2000

                Console.WriteLine(await sr.ReadToEndAsync());
                throw new InvalidOperationException(resp.FunctionError);
            }

            Console.WriteLine("Database initialized successfully");
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
            await WaitForServices("lambda", "resourcegroupstaggingapi", "secretsmanager");

            if (await HasAppResources())
            {
                Console.WriteLine("LocalStack already initialized, terminating...");
                return;
            }

            await SetupDbMigratorLambda();

            await SetupSecrets();

            await InitDb();

            Console.WriteLine("LocalStack initialized successfully :)");
        }
    }
}
