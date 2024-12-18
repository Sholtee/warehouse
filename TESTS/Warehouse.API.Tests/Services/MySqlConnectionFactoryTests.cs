using System;
using System.Data.Common;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using MySql.Data.MySqlClient;
using NUnit.Framework;


namespace Warehouse.API.Services.Tests
{
    [TestFixture]
    internal sealed class MySqlConnectionFactoryTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IMemoryCache> _mockMemoryCache = null!;
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private Mock<Func<string, DbConnection>> _mockCreateConnection = null!;

        MySqlConnectionFactory _connectionFactory = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _mockMemoryCache = new Mock<IMemoryCache>(MockBehavior.Strict);
            _mockSecretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            _mockCreateConnection = new Mock<Func<string, DbConnection>>(MockBehavior.Strict);

            _connectionFactory = new MySqlConnectionFactory
            (
                _mockConfiguration.Object,
                _mockMemoryCache.Object,
                _mockSecretsManager.Object
            )
            {
                CreateConnectionCore = _mockCreateConnection.Object
            };
        }

        [Test]
        public void CreateConnection_ShouldCreateANewConnection()
        {
            Mock<IConfigurationSection> mockPrefix = new(MockBehavior.Strict);
            mockPrefix
                .SetupGet(s => s.Value)
                .Returns("local");
            mockPrefix
                .SetupGet(s => s.Path)
                .Returns((string)null!);

            _mockConfiguration
                .Setup(c => c.GetSection("Prefix"))
                .Returns(mockPrefix.Object);
            _mockConfiguration
                .Setup(c => c.GetSection($"{nameof(MySqlConnectionFactory)}:CacheExpirationMinutes"))
                .Returns(new Mock<IConfigurationSection>(MockBehavior.Loose).Object);

            object? result;
            _mockMemoryCache
                .Setup(c => c.TryGetValue("db-secret", out result))
                .Returns(false);

            Mock<ICacheEntry> mockCacheEntry = new(MockBehavior.Loose);
            _mockMemoryCache
                .Setup(c => c.CreateEntry("db-secret"))
                .Returns(mockCacheEntry.Object);

            _mockSecretsManager
                .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-db-secret"), default))
                .ReturnsAsync
                (
                    new GetSecretValueResponse
                    {
                        SecretString = JsonSerializer.Serialize
                        (
                            new
                            {
                                Endpoint = "endpoint",
                                Database = "db",
                                UserName = "user",
                                Password = "pass"
                            }
                        )
                    }
                );

            Mock<DbConnection> mockDbConnection = new(MockBehavior.Strict);
            mockDbConnection
                .Setup(c => c.OpenAsync(default))
                .Returns(Task.CompletedTask);

            _mockCreateConnection
                .Setup
                (
                    c => c.Invoke
                    (
                        new MySqlConnectionStringBuilder
                        {
                            Server = "endpoint",
                            UserID = "user",
                            Password = "pass",
                            Database = "db"
                        }.ConnectionString
                    )
                )
                .Returns( mockDbConnection.Object );

            Assert.That(_connectionFactory.CreateConnection(), Is.EqualTo(mockDbConnection.Object));

            _mockCreateConnection.Verify(c => c.Invoke(It.IsAny<string>()), Times.Once);
            _mockMemoryCache.Verify(c => c.TryGetValue(It.IsAny<string>(), out result), Times.Once);
            _mockSecretsManager.Verify(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Once); 
        }
    }
}
