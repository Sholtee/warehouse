/********************************************************************************
* MySqlConnectionFactoryTests.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Data;
using System.Data.Common;
using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using StackExchange.Profiling.Data;


namespace Warehouse.Host.Services.Tests
{
    [TestFixture]
    internal sealed class MySqlConnectionFactoryTests
    {
        private Mock<IHostEnvironment> _mockHostEnvironment = null!;
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private Mock<ILoggerFactory> _mockLoggerFactory = null!;
        private Mock<IOptions<JsonOptions>> _mockJsonOptions = null!;
        private Mock<DbDataSource> _mockDataSource = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockHostEnvironment = new(MockBehavior.Strict);
            _mockHostEnvironment
                .SetupGet(e => e.EnvironmentName)
                .Returns("local");

            _mockSecretsManager = new(MockBehavior.Strict);
            _mockLoggerFactory = new(MockBehavior.Strict);
            _mockDataSource = new(MockBehavior.Strict);
            _mockJsonOptions = new(MockBehavior.Strict);
        }

        [Test]
        public void CreateConnection_ShouldCreateANewConnection()
        {
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
                                host = "endpoint",
                                port = 3306,
                                dbName = "db",
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
                _mockHostEnvironment.Object,
                _mockSecretsManager.Object,
                _mockJsonOptions.Object,
                _mockLoggerFactory.Object
            );

            Assert.That(connectionFactory.DataSource.ConnectionString, Is.EqualTo("Server=endpoint;Port=3306;User ID=user;Password=pass;Database=db"));
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

            ProfiledDbConnection? wrap = connectionFactory.CreateConnection() as ProfiledDbConnection;

            Assert.Multiple(() =>
            {
                Assert.That(wrap is not null);
                Assert.That(wrap!.WrappedConnection, Is.EqualTo(mockConnection.Object));
            });

            mockConnection.Verify(c => c.Open(), Times.Once);
        }
    }
}
