/********************************************************************************
* MySqlConnectionFactoryTests.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Data.Common;
using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    [TestFixture]
    internal sealed class MySqlConnectionFactoryTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private Mock<ILoggerFactory> _mockLoggerFactory = null!;
        private Mock<IOptions<JsonOptions>> _mockJsonOptions = null!;
        private Mock<DbDataSource> _mockDataSource = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _mockSecretsManager = new Mock<IAmazonSecretsManager>(MockBehavior.Strict);
            _mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            _mockDataSource = new Mock<DbDataSource>(MockBehavior.Strict);
            _mockJsonOptions = new Mock<IOptions<JsonOptions>>(MockBehavior.Strict);
        }

        [Test]
        public void CreateConnection_ShouldCreateANewConnection()
        {
            Mock<IConfigurationSection> mockEnv = new(MockBehavior.Strict);
            mockEnv
                .SetupGet(s => s.Value)
                .Returns("local");
            mockEnv
                .SetupGet(s => s.Path)
                .Returns((string)null!);

            _mockConfiguration
                .Setup(c => c.GetSection("ASPNETCORE_ENVIRONMENT"))
                .Returns(mockEnv.Object);

            _mockSecretsManager
                .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-warehouse-db-secret"), default))
                .ReturnsAsync
                (
                    new GetSecretValueResponse
                    {
                        SecretString = JsonSerializer.Serialize
                        (
                            new
                            {
                                endpoint = "endpoint",
                                database = "db",
                                userName = "user",
                                password = "pass"
                            }
                        )
                    }
                );

            _mockJsonOptions
                .SetupGet(j => j.Value)
                .Returns(new JsonOptions());

            _mockLoggerFactory
                .Setup(l => l.CreateLogger(It.IsAny<string>()))
                .Returns(new Mock<ILogger>(MockBehavior.Loose).Object);

            using MySqlConnectionFactory connectionFactory = new
            (
                _mockConfiguration.Object,
                _mockSecretsManager.Object,
                _mockJsonOptions.Object,
                _mockLoggerFactory.Object
            );

            Assert.That(connectionFactory.DataSource.ConnectionString, Is.EqualTo("Server=endpoint;User ID=user;Password=pass;Database=db"));
            _mockSecretsManager.Verify(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Once);

            Mock<DbConnection> mockConnection = new(MockBehavior.Strict);
            mockConnection.Setup(c => c.Open());
            mockConnection
                .Protected()
                .Setup("Dispose", ItExpr.IsAny<bool>());

            _mockDataSource
                .Protected()
                .Setup<DbConnection>("CreateDbConnection")
                .Returns(mockConnection.Object);
            _mockDataSource
                .Protected()
                .Setup("Dispose", ItExpr.IsAny<bool>());

            connectionFactory.DataSource = _mockDataSource.Object;

            Assert.That(connectionFactory.CreateConnection(), Is.EqualTo(mockConnection.Object));
            mockConnection.Verify(c => c.Open(), Times.Once);
        }
    }
}
