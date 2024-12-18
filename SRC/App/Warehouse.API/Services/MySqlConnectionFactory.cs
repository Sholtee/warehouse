using System;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Warehouse.API.Services
{
    using Extensions;

    internal sealed class MySqlConnectionFactory(IConfiguration configuration, IMemoryCache cache, IAmazonSecretsManager secretsManager)
    {
        private sealed record DbSecret(string Endpoint, string Database, string UserName, string Password);

        public Func<string, DbConnection> CreateConnectionCore { get; init; } = connectionString => new MySqlConnection(connectionString); // for tests

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            DbSecret secret = (await cache.GetOrCreateAsync("db-secret", async entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes
                (
                    configuration.GetValue($"{nameof(MySqlConnectionFactory)}:CacheExpirationMinutes", 30)
                );

                GetSecretValueResponse resp = await secretsManager.GetSecretValueAsync
                (
                    new GetSecretValueRequest
                    {
                        SecretId = $"{configuration.GetRequiredValue<string>("Prefix")}-db-secret"
                    }
                );

                return JsonSerializer.Deserialize<DbSecret>
                (
                    resp.SecretString,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                )!;
            }))!;

            DbConnection conn = CreateConnectionCore
            (
                new MySqlConnectionStringBuilder
                {
                    Server = secret.Endpoint,
                    UserID = secret.UserName,
                    Password = secret.Password,
                    Database = secret.Database
                }.ConnectionString
            );

            await conn.OpenAsync();
            return conn;
        }

        public IDbConnection CreateConnection() => CreateConnectionAsync().GetAwaiter().GetResult();
    }
}
