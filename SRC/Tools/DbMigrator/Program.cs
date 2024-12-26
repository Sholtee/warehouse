/********************************************************************************
* Program.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DbUp;
using DbUp.Engine;
using MySqlConnector;

using static System.Environment;

namespace DbMigrator
{
    internal static class Program
    {
        #pragma warning disable CA1812  // this class is instantiated by the JsonSerializer
        private sealed record DbSecret(string Endpoint, string Database, string UserName, string Password);
        #pragma warning restore CA1812

        private static async Task<bool> WaitForServer(string connectionString)
        {
            using MySqlConnection connection = new(connectionString);

            for (int i = 0; i < int.Parse(GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "10", null); i++)
            {
                await Task.Delay(2000 * i);

                try
                {
                    await connection.OpenAsync();

                    Console.WriteLine($"MySQL {connection.ServerVersion} is READY");
                    return true;
                }
                catch (MySqlException ex)
                {
                    Console.WriteLine($"Failed to connect to MySQL: {ex.Message}");
                    continue;
                }
            }

            return false;
        }

        static async Task<int> Main()
        {
            DbSecret secret;

            using (AmazonSecretsManagerClient client = new())
            {
                secret = JsonSerializer.Deserialize<DbSecret>
                (
                    (
                        await client.GetSecretValueAsync
                        (
                            new GetSecretValueRequest
                            {
                                SecretId = $"{GetEnvironmentVariable("PREFIX")}-warehouse-db-secret"
                            }
                        )
                    ).SecretString,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                )!;
            }

            MySqlConnectionStringBuilder connectionStringBuilder = new()
            {
                Server = secret.Endpoint,
                UserID = secret.UserName,
                Password = secret.Password
            };

            //
            // Wait for the server to be ready
            //

            if (!await WaitForServer(connectionStringBuilder.ConnectionString))
                return -1;

            connectionStringBuilder.Database = secret.Database;

            //
            // Create the database if necessary
            //

            EnsureDatabase.For.MySqlDatabase(connectionStringBuilder.ConnectionString);

            //
            // Perform the upgrade
            //

            UpgradeEngine upgrader = DeployChanges
                .To
                .MySqlDatabase(connectionStringBuilder.ConnectionString)
                .WithScriptsFromFileSystem("Evolutions")
                .LogToConsole()
                .Build();

            DatabaseUpgradeResult result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                Console.WriteLine(result.Error);
                return -1;
            }

            Console.WriteLine("Migration successful :)");
            return 0;
        }
    }
}
