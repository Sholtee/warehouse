/********************************************************************************
* LoginControllerTests.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace Warehouse.API.Controllers.Tests
{
    using Core.Abstractions;
    using Core.Auth;
    using Core.Exceptions;
    using DAL;

    [TestFixture]
    internal class LoginControllerTests
    {
        private LoginController _loginController = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<ITokenManager> _mockTokenManager = null!;
        private Mock<IPasswordHasher<string>> _mockPasswordHasher = null!;
        private Mock<ILogger<LoginController>> _mockLogger = null!;
        private Mock<ISessionManager> _mockSessionManager = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _mockTokenManager = new Mock<ITokenManager>(MockBehavior.Strict);
            _mockPasswordHasher = new Mock<IPasswordHasher<string>>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<LoginController>>(MockBehavior.Loose);
            _mockSessionManager = new Mock<ISessionManager>(MockBehavior.Strict);

            _loginController = new LoginController
            (
                _mockUserRepository.Object,
                _mockTokenManager.Object,
                _mockSessionManager.Object,
                _mockPasswordHasher.Object,
                _mockLogger.Object
            );
            _loginController.ControllerContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public void Login_ShouldCreateANewSessionCookie()
        {
            const string
                CLIENT_ID = "test_user",
                CLIENT_SECRET = "test_pw";

            User user = new() { ClientId = CLIENT_ID, ClientSecretHash = "hash", Roles = Roles.User };

            _mockUserRepository
                .Setup(r => r.QueryUser(CLIENT_ID))
                .ReturnsAsync(user);

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword(CLIENT_ID, "hash", CLIENT_SECRET))
                .Returns(PasswordVerificationResult.Success);

            Mock<IConfigurationSection> mockExpiration = new(MockBehavior.Strict);
            mockExpiration
                .SetupGet(s => s.Value)
                .Returns("30");
            mockExpiration
                .SetupGet(s => s.Path)
                .Returns((string)null!);

            Mock<IConfigurationSection> mockCookieName = new(MockBehavior.Strict);
            mockCookieName
                .SetupGet(s => s.Value)
                .Returns("session-cookie");
            mockCookieName
                .SetupGet(s => s.Path)
                .Returns((string)null!);

            _mockConfiguration
                .Setup(c => c.GetSection("Auth:SessionExpirationMinutes"))
                .Returns(mockExpiration.Object);
            _mockConfiguration
                .Setup(c => c.GetSection("Auth:SessionCookieName"))
                .Returns(mockCookieName.Object);

            _mockSessionManager
                .SetupSet(s => s.Token = "token");

            _mockTokenManager
                .Setup(s => s.CreateTokenAsync(CLIENT_ID, user.Roles))
                .ReturnsAsync("token");

            _loginController.Request.Headers.Add
            (
                new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Basic " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}")
                        )
                    )
                )
            );

            Assert.DoesNotThrowAsync(_loginController.Login);

            _mockUserRepository.Verify(r => r.QueryUser(CLIENT_ID), Times.Once);
            _mockPasswordHasher.Verify(h => h.VerifyHashedPassword(CLIENT_ID, "hash", CLIENT_SECRET), Times.Once);
            _mockTokenManager.Verify(s => s.CreateTokenAsync(CLIENT_ID, user.Roles), Times.Once);
            _mockSessionManager.VerifySet(s => s.Token = "token", Times.Once);
        }

        public static IEnumerable<KeyValuePair<string, StringValues>> InvalidLoginHeaders
        {
            get
            {
                yield return new KeyValuePair<string, StringValues>
                (
                    "Invalid",
                    new StringValues
                    (
                        "Basic " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes($"user:pass")
                        )
                    )
                );

                yield return new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Invalid " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes($"user:pass")
                        )
                    )
                );

                yield return new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes($"user:pass")
                        )
                    )
                );

                yield return new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Basic invalid"
                    )
                );

                yield return new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Basic " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes($"no_password")
                        )
                    )
                );
            }
        }

        [TestCaseSource(nameof(InvalidLoginHeaders))]
        public void Login_ShouldReturnUnauthorizedOnInvalidLoginRequest(KeyValuePair<string, StringValues> header)
        {
            _loginController.Request.Headers.Add(header);

            Assert.ThrowsAsync<UnauthorizedException>(_loginController.Login);
        }

        [Test]
        public void Login_ShouldReturnUnauthorizedOnMissingUser()
        {
            _mockUserRepository
                .Setup(r => r.QueryUser("user"))
                .ReturnsAsync((User)null!);

            _loginController.Request.Headers.Add
            (
                new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Basic " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes("user:pass")
                        )
                    )
                )
            );

            Assert.ThrowsAsync<UnauthorizedException>(_loginController.Login);
            _mockUserRepository.Verify(r => r.QueryUser("user"), Times.Once);
        }

        [Test]
        public void Login_ShouldReturnUnauthorizedOnInvalidCreds()
        {
            _mockUserRepository
                .Setup(r => r.QueryUser("user"))
                .ReturnsAsync(new User() { ClientId = "user", ClientSecretHash = "hash", Roles = Roles.User });

            _mockPasswordHasher
                .Setup(h => h.VerifyHashedPassword("user", "hash", "pass"))
                .Returns(PasswordVerificationResult.Failed);

            _loginController.Request.Headers.Add
            (
                new KeyValuePair<string, StringValues>
                (
                    "Authorization",
                    new StringValues
                    (
                        "Basic " + Convert.ToBase64String
                        (
                            Encoding.UTF8.GetBytes("user:pass")
                        )
                    )
                )
            );

            Assert.ThrowsAsync<UnauthorizedException>(_loginController.Login);

            _mockUserRepository.Verify(r => r.QueryUser("user"), Times.Once);
            _mockPasswordHasher.Verify(h => h.VerifyHashedPassword("user", "hash", "pass"), Times.Once);
        }

        [Test]
        public void Logout_ShouldRemoveTheSessionCookie()
        {
            Mock<IConfigurationSection> mockCookieName = new(MockBehavior.Strict);
            mockCookieName
                .SetupGet(s => s.Value)
                .Returns("session-cookie");

            _mockSessionManager
                .SetupGet(s => s.Token)
                .Returns("token");
            _mockSessionManager
                .SetupSet(s => s.Token = null);

            _mockTokenManager
                .Setup(t => t.RevokeTokenAsync("token"))
                .ReturnsAsync(true);

            Assert.DoesNotThrowAsync(_loginController.Logout);

            _mockTokenManager.Verify(t => t.RevokeTokenAsync("token"), Times.Once);
            _mockSessionManager.VerifySet(s => s.Token = null, Times.Once);
        }
    }
}
