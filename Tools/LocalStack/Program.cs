using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using static System.Environment;

namespace LocalStack.Setup
{
    internal class Program
    {
        private sealed class LocalStackStatus
        {
            public required Dictionary<string, string> Services { get; init; }
            public required string Edition { get; init; }
            public required string Version { get; init; }
        }

        private static async Task WiatForServices(string endPoint, params string[] services)
        {
            //
            // Wait for LocalStack to be ready
            //

            using HttpClient httpClient = new();

            httpClient.BaseAddress = new Uri(endPoint);

            JsonSerializerOptions serializerOpts = new()
            {
                PropertyNameCaseInsensitive = true
            };

            for (int i = 0; i < int.Parse(GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "10"); i++)
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
                        if (status?.Services.TryGetValue(service, out string? serviceStatus) is not true || serviceStatus != "available")
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

        public static async Task Main()
        {
            string endPoint = GetEnvironmentVariable("LOCALSTACK_ENDPOINT_URL") ?? "http://localhost:4566";

            await WiatForServices(endPoint, "secretsmanager");

            using IAmazonSecretsManager client = new AmazonSecretsManagerClient
            (
                new BasicAWSCredentials
                (
                    GetEnvironmentVariable("LOCALSTACK_ACCESS_KEY") ?? "LOCAL",
                    GetEnvironmentVariable("LOCALSTACK_SECRET_KEY") ?? "LOCAL"
                ),
                new AmazonSecretsManagerConfig
                {
                    ServiceURL = endPoint
                }
            );

            Dictionary<string, SecretListEntry> secrets = (await client.ListSecretsAsync(new ListSecretsRequest { })).SecretList.ToDictionary(static s => s.Name);

            await SetupSecret("local-api-users", new Dictionary<string, string>
            {
                [GetEnvironmentVariable("ADMIN_USER") ?? "admin"] = new PasswordHasher<object>().HashPassword(null!, GetEnvironmentVariable("ADMIN_PW") ?? "admin")
            });

            await SetupSecret("local-api-certificate", new Dictionary<string, string>
            {
                ["privateKey"] = File.ReadAllText(Path.Combine("Cert", "private.key")),
                ["certificate"] = File.ReadAllText(Path.Combine("Cert", "certificate.crt"))
            });

            async Task SetupSecret(string name, Dictionary<string, string> value)
            {
                Console.WriteLine($"Setup {name}...");

                if (secrets.ContainsKey(name))
                {
                    Console.WriteLine($"{name} already exists");
                    return;
                }

                await client.CreateSecretAsync
                (
                    new CreateSecretRequest
                    {
                        Name = name,
                        SecretString = JsonSerializer.Serialize(value)
                    }
                );
            }

            Console.WriteLine("All OK :)");
        }
    }
}
