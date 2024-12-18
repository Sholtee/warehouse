using System.Linq;
using System.Text.RegularExpressions;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;


namespace Warehouse.API.Services.Tests
{
    using DAL;

    [TestFixture]
    internal class RootUserRegistrarTests
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
                .Setup
                (
                    h => h.HashPassword
                    (
                        "root",
                        It.Is<string>
                        (
                            s => s.Length == 20 &&
                                Regex.Match(s, "\\d+").Success &&
                                Regex.Match(s, "[a-z]").Success &&
                                Regex.Match(s, "[A-Z]").Success &&
                                Regex.Match(s, "[!@#$%^&*()_+]").Success
                        )
                    )
                )
                .Returns("hash");

            Mock<IConfigurationSection> mockPrefix = new(MockBehavior.Strict);
            mockPrefix
                .SetupGet(s => s.Value)
                .Returns("local");
            mockPrefix
                .SetupGet(s => s.Path)
                .Returns((string) null!);

            _mockConfiguration
                .Setup(c => c.GetSection("Prefix"))
                .Returns(mockPrefix.Object);

            _mockSecretsManager
                .Setup
                (
                    s => s.CreateSecretAsync
                    (
                        It.Is<CreateSecretRequest>(r => r.Name == "local-root-user-creds" & r.SecretString == _mockPasswordHasher.Invocations[0].Arguments[1].ToString()),
                        default
                    )
                )
                .ReturnsAsync((CreateSecretResponse) null!);

            Assert.That(_rootUserRegistrar.EnsureHasRootUser(), Is.True);
            Assert.That(_rootUserRegistrar.EnsureHasRootUser(), Is.False);

            _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<CreateUserParam>()), Times.Exactly(2));
            _mockSecretsManager.Verify(s => s.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), default), Times.Once);
        }
    }
}
