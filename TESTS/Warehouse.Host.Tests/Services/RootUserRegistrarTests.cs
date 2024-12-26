/********************************************************************************
* RootUserRegistrarTests.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Linq;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    using DAL;

    [TestFixture]
    internal sealed class RootUserRegistrarTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<ILogger<RootUserRegistrar>> _mockLogger = null!;
        private Mock<IPasswordHasher<string>> _mockPasswordHasher = null!;
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private RootUserRegistrar _rootUserRegistrar = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockConfiguration = new(MockBehavior.Strict);
            _mockUserRepository = new(MockBehavior.Strict);
            _mockLogger = new(MockBehavior.Loose);
            _mockPasswordHasher = new(MockBehavior.Strict);
            _mockSecretsManager = new(MockBehavior.Strict);
            _rootUserRegistrar = new RootUserRegistrar
            (
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockUserRepository.Object,
                _mockPasswordHasher.Object,
                _mockSecretsManager.Object
            );
        }

        [Test]
        public void EnsureHasRootUser_ShouldCreateTheRootUser()
        {
            bool userExists = false;

            _mockUserRepository
                .Setup(r => r.CreateUser(It.Is<CreateUserParam>(p => p.ClientId == "root" && p.ClientSecretHash == "hash" && p.Groups.Contains("Admins"))))
                .ReturnsAsync(() =>
                {
                    bool res = !userExists;
                    userExists = true;
                    return res;
                });

            _mockPasswordHasher
                .Setup(h => h.HashPassword("root", "password"))
                .Returns("hash");

            Mock<IConfigurationSection> mockEnv = new(MockBehavior.Strict);
            mockEnv
                .SetupGet(s => s.Value)
                .Returns("local");
            mockEnv
                .SetupGet(s => s.Path)
                .Returns((string) null!);

            _mockConfiguration
                .Setup(c => c.GetSection("ASPNETCORE_ENVIRONMENT"))
                .Returns(mockEnv.Object);

            _mockSecretsManager
                .Setup
                (
                    s => s.GetSecretValueAsync
                    (
                        It.Is<GetSecretValueRequest>(r => r.SecretId == "local-warehouse-root-user-password"),
                        default
                    )
                )
                .ReturnsAsync(new GetSecretValueResponse
                {
                    SecretString = "password"
                });

            Assert.That(_rootUserRegistrar.EnsureHasRootUser(), Is.True);
            Assert.That(_rootUserRegistrar.EnsureHasRootUser(), Is.False);

            _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<CreateUserParam>()), Times.Exactly(2));
            _mockSecretsManager.Verify(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Exactly(2));
        }
    }
}
