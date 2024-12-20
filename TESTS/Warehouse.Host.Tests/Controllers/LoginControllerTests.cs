using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using NUnit.Framework;

namespace Warehouse.API.Controllers.Tests
{
    using Core.Abstractions;
    using Core.Auth;
    using DAL;

    [TestFixture]
    internal class LoginControllerTests
    {
        private LoginController _loginController = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IJwtService> _mockIJwtService = null!;
        private Mock<IPasswordHasher<string>> _mockPasswordHasher = null!;
        private Mock<ILogger<LoginController>> _mockLogger = null!;
        private Mock<TimeProvider> _mockTimeProvider = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockUserRepository = new Mock<IUserRepository>(MockBehavior.Strict);
            _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _mockIJwtService = new Mock<IJwtService>(MockBehavior.Strict);
            _mockPasswordHasher = new Mock<IPasswordHasher<string>>(MockBehavior.Strict);
            _mockLogger = new Mock<ILogger<LoginController>>(MockBehavior.Loose);
            _mockTimeProvider = new Mock<TimeProvider>(MockBehavior.Strict);

            _loginController = new LoginController
            (
                _mockUserRepository.Object,
                _mockConfiguration.Object,
                _mockIJwtService.Object,
                _mockPasswordHasher.Object,
                _mockLogger.Object,
                _mockTimeProvider.Object
            );
            _loginController.ControllerContext.HttpContext = new DefaultHttpContext();
        }

        [Test]
        public async Task Login_ShouldCreateANewSessionCookie()
        {
            const string
                CLIENT_ID = "test_user",
                CLIENT_SECRET = "test_pw";

            QueryUserResult user = new() { ClientId = CLIENT_ID, ClientSecretHash = "hash", Roles = Roles.User };

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

            DateTimeOffset now = new(1986, 10, 26, 0, 0, 0, TimeSpan.Zero);

            _mockTimeProvider
                .Setup(t => t.GetUtcNow())
                .Returns(now);

            _mockIJwtService
                .Setup(s => s.CreateTokenAsync(CLIENT_ID, user.Roles, now.AddMinutes(30)))
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

            IActionResult result = await _loginController.Login();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NoContentResult>());
                Assert.That
                (
                    _loginController.Response.Headers.SetCookie.ToString(),
                    Is.EqualTo("session-cookie=token; expires=Sun, 26 Oct 1986 00:30:00 GMT; path=/; secure; samesite=strict; httponly")
                );
            });

            _mockUserRepository.Verify(r => r.QueryUser(CLIENT_ID), Times.Once);
            _mockPasswordHasher.Verify(h => h.VerifyHashedPassword(CLIENT_ID, "hash", CLIENT_SECRET), Times.Once);
            _mockIJwtService.Verify(s => s.CreateTokenAsync(CLIENT_ID, user.Roles, now.AddMinutes(30)), Times.Once);
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
        public async Task Login_ShouldReturnUnauthorizedOnInvalidLoginRequest(KeyValuePair<string, StringValues> header)
        {
            _loginController.Request.Headers.Add(header);

            IActionResult result = await _loginController.Login();

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task Login_ShouldReturnUnauthorizedOnMissingUser()
        {
            _mockUserRepository
                .Setup(r => r.QueryUser("user"))
                .ReturnsAsync((QueryUserResult)null!);

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

            IActionResult result = await _loginController.Login();

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
            _mockUserRepository.Verify(r => r.QueryUser("user"), Times.Once);
        }

        [Test]
        public async Task Login_ShouldReturnUnauthorizedOnInvalidCreds()
        {
            _mockUserRepository
                .Setup(r => r.QueryUser("user"))
                .ReturnsAsync(new QueryUserResult() { ClientId = "user", ClientSecretHash = "hash", Roles = Roles.User });

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

            IActionResult result = await _loginController.Login();

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());

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

            _mockConfiguration
                .Setup(c => c.GetSection("Auth:SessionCookieName"))
                .Returns(mockCookieName.Object);

            _loginController.Request.Headers.Add
            (
                new KeyValuePair<string, StringValues>
                (
                    "Cookie",
                    new StringValues("session-cookie=token")
                )
            );

            IActionResult result = _loginController.Logout();

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<NoContentResult>());
                Assert.That
                (
                    _loginController.Response.Headers.SetCookie.ToString(),
                    Is.EqualTo("session-cookie=; expires=Thu, 01 Jan 1970 00:00:00 GMT; path=/")
                );
            });
        }
    }
}
