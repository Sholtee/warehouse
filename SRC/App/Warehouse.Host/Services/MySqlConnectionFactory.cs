/********************************************************************************
* MySqlConnectionFactory.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace Warehouse.Host.Services
{
    using Core.Extensions;

    internal sealed class MySqlConnectionFactory: IDisposable
    {
        private sealed record DbSecret(string Endpoint, string Database, string UserName, string Password);

        public MySqlConnectionFactory(IConfiguration configuration, IAmazonSecretsManager secretsManager, ILoggerFactory logger)
        {
            GetSecretValueResponse resp = secretsManager.GetSecretValueAsync
            (
                new GetSecretValueRequest
                {
                    SecretId = $"{configuration.GetRequiredValue<string>("Prefix")}-db-secret"
                }
            ).GetAwaiter().GetResult();

            DbSecret secret = JsonSerializer.Deserialize<DbSecret>
            (
                resp.SecretString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            )!;

            DataSource = new MySqlDataSourceBuilder
            (
                new MySqlConnectionStringBuilder
                {
                    Server = secret.Endpoint,
                    UserID = secret.UserName,
                    Password = secret.Password,
                    Database = secret.Database
                }.ConnectionString
            )
            .UseLoggerFactory(logger)
            .Build();
        }

        public void Dispose()
        {
            DataSource?.Dispose();
            DataSource = null!;
        }

        public DbDataSource DataSource { get; set; }  // for tests

        public IDbConnection CreateConnection()
        {
            DbConnection connection = DataSource.CreateConnection();
            connection.Open();
            return connection;
        }
    }
}
