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
using System.Text.Json.Serialization;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Warehouse.Host.Services
{
    internal sealed class MySqlConnectionFactory: IDisposable
    {
        private sealed record DbSecret
        (
            string Host,
            uint Port,
            string DbName,
            string UserName,
            string Password
        );

        #pragma warning disable CA2213 // This field is disposed properly
        private DbDataSource _dataSource;
        #pragma warning restore CA2213

        public MySqlConnectionFactory(IHostEnvironment env, IAmazonSecretsManager secretsManager, IOptions<JsonOptions> jsonOptions, ILoggerFactory logger)
        {
            GetSecretValueResponse resp = secretsManager.GetSecretValueAsync
            (
                new GetSecretValueRequest
                {
                    SecretId = $"{env.EnvironmentName}-warehouse-db-secret"
                }
            ).GetAwaiter().GetResult();

            DbSecret secret = JsonSerializer.Deserialize<DbSecret>
            (
                resp.SecretString,
                new JsonSerializerOptions(jsonOptions.Value.JsonSerializerOptions)
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip  // we dont need the cluster id, etc
                }
            )!;

            _dataSource = new MySqlDataSourceBuilder
            (
                new MySqlConnectionStringBuilder
                {
                    Server = secret.Host,
                    Port = secret.Port,
                    UserID = secret.UserName,
                    Password = secret.Password,
                    Database = secret.DbName
                }.ConnectionString
            )
            .UseLoggerFactory(logger)
            .Build();
        }

        public void Dispose() => DataSource = null!;

        public DbDataSource DataSource // for tests
        {
            get => _dataSource;
            set
            {
                _dataSource?.Dispose();
                _dataSource = value;
            }
        }  

        public IDbConnection CreateConnection()
        {
            DbConnection connection = DataSource.CreateConnection();
            connection.Open();

            //
            // ProfiledDbConnection does nothing if the underlying profiler is not active
            // (MiniProfiler.Current != null)
            //

            return new ProfiledDbConnection(connection, MiniProfiler.Current);
        }
    }
}
