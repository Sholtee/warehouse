using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using MySqlConnector;

using static System.Environment;

namespace DbMigrator
{
    internal class Program
    {
        private sealed record DbSecret(string Endpoint, string Database, string UserName, string Password);

        private static async Task WaitForServer(string connectionString)
        {
            using MySqlConnection connection = new(connectionString);

            for (int i = 0; i < int.Parse(GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "10"); i++)
            {
                await Task.Delay(2000 * i);

                try
                {
                    await connection.OpenAsync();

                    Console.WriteLine($"Connection to MySQL {connection.ServerVersion} successful");
                    return;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }

            throw new InvalidOperationException("Failed to connect to MySQL");
        }

        static async Task Main()
        {
            DbSecret secret;

            using (IAmazonSecretsManager client = new AmazonSecretsManagerClient())
            {
                secret = JsonSerializer.Deserialize<DbSecret>
                (
                    (
                        await client.GetSecretValueAsync
                        (
                            new GetSecretValueRequest
                            {
                                SecretId = $"{GetEnvironmentVariable("PREFIX")}-db-secret"
                            }
                        )
                    ).SecretString,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                )!;
            }

            string connectionString = new MySqlConnectionStringBuilder
            {
                Server = secret.Endpoint,
                UserID = secret.UserName,
                Password = secret.Password,
                Database = secret.Database
            }.ConnectionString;

            await WaitForServer(connectionString);


        }
    }
}
