/********************************************************************************
* Function.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DbUp;
using DbUp.Engine;
using MySqlConnector;

using static System.Environment;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace DbMigrator
{
    public sealed class LambdaFunction
    {
        #pragma warning disable CA1812  // this class is instantiated by the JsonSerializer
        private sealed record DbSecret(string Host, uint Port, string DbName, string UserName, string Password);
        #pragma warning restore CA1812

        private static async Task<bool> WaitForServer(string connectionString, ILambdaLogger logger)
        {
            using MySqlConnection connection = new(connectionString);

            for (int i = 0; i < int.Parse(GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "10", null); i++)
            {
                await Task.Delay(2000 * i);

                try
                {
                    await connection.OpenAsync();

                    logger.LogInformation("MySQL {serverVersion} is READY", connection.ServerVersion);
                    return true;
                }
                catch (MySqlException ex)
                {
                    logger.LogWarning("Failed to connect to MySQL host: {error}", ex.Message);
                    continue;
                }
            }

            return false;
        }

        #pragma warning disable CA1822 // Lambda handler cannot be static
        public async Task Handler(object? input, ILambdaContext context)
        #pragma warning restore CA1822
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

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
                Server = secret.Host,
                Port = secret.Port,
                UserID = secret.UserName,
                Password = secret.Password
            };

            //
            // Wait for the server to be ready
            //

            if (!await WaitForServer(connectionStringBuilder.ConnectionString, context.Logger))
            {
                context.Logger.LogError("Failed to connect to MySQL host");
                return;
            }

            connectionStringBuilder.Database = secret.DbName;

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
                context.Logger.LogError(result.Error, "Migration failed :(");
                return;
            }

            context.Logger.LogInformation("Migration successful :)");
        }
    }
}
