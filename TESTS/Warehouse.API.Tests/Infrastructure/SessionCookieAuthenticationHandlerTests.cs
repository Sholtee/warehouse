using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;

namespace Warehouse.API.Infrastructure.Tests
{
    using Auth;
    using Services;

    [TestFixture]
    internal class SessionCookieAuthenticationHandlerTests
    {
        private Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptionsMonitor = null!;
        private Mock<ILoggerFactory> _mockLoggerFactory = null!;
        private Mock<IJwtService> _mockJwtService = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<UrlEncoder> _mockUrlEncoder = null!;
        private Mock<ILogger> _mockLogger = null!;
        private Mock<TimeProvider> _mockTimeProvider = null!;

        private HttpContext _context = null!;
        private SessionCookieAuthenticationHandler _handler = null!;

        [SetUp]
        public async Task SetupTest()
        {
            Mock<IConfigurationSection> mockCookieName = new(MockBehavior.Strict);
            mockCookieName
                .SetupGet(s => s.Value)
                .Returns("session-cookie");

            _mockTimeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
            _mockOptionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>(MockBehavior.Strict);
            _mockOptionsMonitor
                .Setup(m => m.Get(SessionCookieAuthenticationHandler.SCHEME))
                .Returns(new AuthenticationSchemeOptions { TimeProvider = _mockTimeProvider.Object });
            _mockLogger = new Mock<ILogger>(MockBehavior.Loose);
            _mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            _mockLoggerFactory
                .Setup(f => f.CreateLogger(typeof(SessionCookieAuthenticationHandler).FullName!))
                .Returns(_mockLogger.Object);
            _mockJwtService = new Mock<IJwtService>(MockBehavior.Strict);
            _mockConfiguration = new Mock<IConfiguration>(MockBehavior.Strict);
            _mockConfiguration
                .Setup(c => c.GetSection("Auth:SessionCookieName"))
                .Returns(mockCookieName.Object);
            _mockUrlEncoder = new Mock<UrlEncoder>(MockBehavior.Strict);

            _context = new DefaultHttpContext();

            _handler = new SessionCookieAuthenticationHandler
            (
                _mockOptionsMonitor.Object,
                _mockLoggerFactory.Object,
                _mockJwtService.Object,
                _mockConfiguration.Object,
                _mockUrlEncoder.Object
            );
            
            await _handler.InitializeAsync
            (
                new AuthenticationScheme
                (
                    SessionCookieAuthenticationHandler.SCHEME,
                    null,
                    typeof(SessionCookieAuthenticationHandler)
                ),
                _context
            );
        }

        [Test]
        public async Task EndpointAllowingAnonAccess()
        {
            _context.SetEndpoint
            (
                new Endpoint
                (
                    null,
                    new EndpointMetadataCollection
                    (
                        new AllowAnonymousAttribute()
                    ),
                    null
                )
            );

            AuthenticateResult result = await _handler.AuthenticateAsync();
            Assert.That(result.None);
        }

        [Test]
        public async Task NoSessionCookie()
        {
            AuthenticateResult result = await _handler.AuthenticateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Failure?.Message, Is.EqualTo("Missing session cookie"));
            });
        }

        [Test]
        public async Task InvalidToken()
        {
            _context.Request.Headers.Append("cookie", new StringValues("session-cookie=token"));

            Exception failure = new Exception("Invalid token");

            _mockJwtService
                .Setup(j => j.ValidateToken("token"))
                .ReturnsAsync(new TokenValidationResult { IsValid = false, Exception = failure });
            
            AuthenticateResult result = await _handler.AuthenticateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Failure, Is.EqualTo(failure));
            });
        }
    }
}
