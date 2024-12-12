using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;

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

        private sealed class UserDescriptor
        {
            public required List<string> Groups { get; init; }
            public required string Password { get; init; }
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

        private static async Task SetupSecrets()
        {
            using IAmazonSecretsManager client = new AmazonSecretsManagerClient();

            Dictionary<string, SecretListEntry> secrets = 
            (
                await client.ListSecretsAsync(new ListSecretsRequest { })
            ).SecretList.ToDictionary(static s => s.Name);

            PasswordHasher<string> pwHasher = new();

            await SetupSecret
            (
                "local-api-users",
                JsonSerializer
                    .Deserialize<Dictionary<string, UserDescriptor>>
                    (
                        File.ReadAllText("users.json")
                    )!
                    .ToDictionary
                    (
                        static e => e.Key,
                        e => (object)new
                        {
                            e.Value.Groups,
                            PasswordHash = pwHasher.HashPassword(e.Key, e.Value.Password)
                        }
                    )
            );

            await SetupSecret("local-api-certificate", new Dictionary<string, string>
            {
                ["privateKey"] = File.ReadAllText(Path.Combine("Cert", "private.key")),
                ["certificate"] = File.ReadAllText(Path.Combine("Cert", "certificate.crt"))
            });

            await SetupSecret("local-jwt-secret-key", "very-very-very-very-very-very-very-secret-key");

            await SetupSecret("local-db-secret", new Dictionary<string, string>
            {
                ["endpoint"] = "aurora",
                ["database"] = "WarehouseDb",
                ["username"] = "root",
                ["password"] = "kerekesfacapa"
            });

            async Task SetupSecret(string name, object value)
            {
                Console.WriteLine($"Setup {name}...");

                if (value is not string valueStr)
                    valueStr = JsonSerializer.Serialize(value);

                if (secrets.ContainsKey(name))
                    await client.PutSecretValueAsync
                    (
                        new PutSecretValueRequest
                        {
                            SecretId = name,
                            SecretString = valueStr
                        }
                    );
                else
                    await client.CreateSecretAsync
                    (
                        new CreateSecretRequest
                        {
                            Name = name,
                            SecretString = valueStr
                        }
                    );
            }
        }

        public static async Task Main()
        {
            await WiatForServices("secretsmanager");

            await SetupSecrets();

            Console.WriteLine("All OK :)");
        }
    }
}
