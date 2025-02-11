/********************************************************************************
* SessionCookieAuthenticationHandlerTests.cs                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Auth;
    using Core.Abstractions;
    using Core.Attributes;
    using Core.Auth;
    using Registrations;
    using Services;

    [TestFixture]
    internal sealed class SessionCookieAuthenticationHandlerTests
    {
        private Mock<IOptionsMonitor<AuthenticationSchemeOptions>> _mockOptionsMonitor = null!;
        private Mock<ILoggerFactory> _mockLoggerFactory = null!;
        private Mock<IJwtService> _mockJwtService = null!;
        private Mock<ISessionManager> _mockSessionManager = null!;
        private Mock<UrlEncoder> _mockUrlEncoder = null!;
        private Mock<ILogger> _mockLogger = null!;
        private Mock<TimeProvider> _mockTimeProvider = null!;

        private HttpContext _context = null!;
        private SessionCookieAuthenticationHandler _handler = null!;

        [SetUp]
        public async Task SetupTest()
        {
            _mockTimeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
            _mockOptionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>(MockBehavior.Strict);
            _mockOptionsMonitor
                .Setup(m => m.Get(WarehouseAuthentication.SCHEME))
                .Returns(new AuthenticationSchemeOptions { TimeProvider = _mockTimeProvider.Object });
            _mockLogger = new Mock<ILogger>(MockBehavior.Loose);
            _mockLoggerFactory = new Mock<ILoggerFactory>(MockBehavior.Strict);
            _mockLoggerFactory
                .Setup(f => f.CreateLogger(typeof(SessionCookieAuthenticationHandler).FullName!))
                .Returns(_mockLogger.Object);
            _mockJwtService = new Mock<IJwtService>(MockBehavior.Strict);
            _mockSessionManager = new Mock<ISessionManager>(MockBehavior.Strict);
            _mockSessionManager
                .SetupGet(c => c.SlidingExpiration)
                .Returns(false);
            _mockUrlEncoder = new Mock<UrlEncoder>(MockBehavior.Strict);

            _context = new DefaultHttpContext();

            _handler = new SessionCookieAuthenticationHandler
            (
                _mockOptionsMonitor.Object,
                _mockLoggerFactory.Object,
                _mockSessionManager.Object,
                _mockJwtService.Object,
                _mockUrlEncoder.Object
            );
            
            await _handler.InitializeAsync
            (
                new AuthenticationScheme
                (
                    WarehouseAuthentication.SCHEME,
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
            _mockSessionManager
                .SetupGet(c => c.Token)
                .Returns((string?) null);

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
            _mockSessionManager
                .SetupGet(s => s.Token)
                .Returns("token");

            Exception failure = new Exception("Invalid token");

            _mockJwtService
                .Setup(j => j.ValidateTokenAsync("token"))
                .ReturnsAsync(new TokenValidationResult { IsValid = false, Exception = failure });
            
            AuthenticateResult result = await _handler.AuthenticateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Failure, Is.EqualTo(failure));
            });
        }

        [Test]
        public async Task ValidToken()
        {
            _mockSessionManager
                .SetupGet(s => s.Token)
                .Returns("token");

            ClaimsIdentity identity = new();

            _mockJwtService
                .Setup(j => j.ValidateTokenAsync("token"))
                .ReturnsAsync(new TokenValidationResult { IsValid = true, ClaimsIdentity = identity });

            AuthenticateResult result = await _handler.AuthenticateAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded);
                Assert.That(result.Ticket?.Principal.Identity, Is.EqualTo(identity));
            });
        }
    }

    [ApiController, Authorize]
    public sealed class AuthTestController : ControllerBase
    {
        [HttpGet("anonaccess")]
        [AllowAnonymous]
        public IActionResult AnonAccess() => Ok();

        [HttpGet("adminaccess")]
        [RequiredRoles(Roles.Admin)]
        public IActionResult AdminAccess() => Ok();
    }

    [TestFixture]
    internal class SessionCookieAuthenticationHandlerIntegrationTests
    {
        private sealed class TestHostFactory() : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            public DateTime Now { get; set; } = DateTime.UtcNow;

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices(services =>
                {
                    Mock<IMemoryCache> mockMemoryCache = new(MockBehavior.Strict);
                    mockMemoryCache
                        .Setup(c => c.CreateEntry(It.IsAny<object>()))
                        .Returns(() => new Mock<ICacheEntry>(MockBehavior.Loose).Object);

                    object? ret;
                    mockMemoryCache
                        .Setup(c => c.TryGetValue(It.IsAny<object>(), out ret))
                        .Returns(false);

                    services.AddSingleton(mockMemoryCache.Object);

                    Mock<IAmazonSecretsManager> mockSecretsManager = new(MockBehavior.Strict);
                    mockSecretsManager
                        .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-warehouse-jwt-secret-key"), default))
                        .ReturnsAsync(new GetSecretValueResponse { SecretString = "very-very-very-very-very-secure-secret-key" });

                    Mock<TimeProvider> mockTimeProvider = new(MockBehavior.Strict);
                    mockTimeProvider
                        .Setup(p => p.GetUtcNow())
                        .Returns(() => Now);

                    services.AddSingleton(mockTimeProvider.Object);
                    services.AddSingleton(mockSecretsManager.Object);
                    services.AddScoped<ISessionManager, HttpSessionManager>();

                    services.AddHttpContextAccessor();
                    services.AddSessionCookieAuthentication();

                    services
                        .AddMvc()
                        .AddApplicationPart(typeof(AuthTestController).Assembly)
                        .AddControllersAsServices();
                })
                .Configure
                (
                    static app => app
                        .UseRouting()
                        .UseAuthorization()
                        .UseEndpoints(static endpoints => endpoints.MapControllers())
                );
        }

        private TestHostFactory _appFactory = null!;

        private async Task<string> CreateToken(string user, Roles role)
        {
            using IServiceScope scope = _appFactory.Services.CreateScope();

            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            return await jwtService.CreateTokenAsync(user, role);
        }

        [SetUp]
        public void SetupTest() => _appFactory = new TestHostFactory();

        [TearDown]
        public void TearDownTest()
        {
            _appFactory?.Dispose();
            _appFactory = null!;
        }

        [Test]
        public async Task AnonAccess()
        {
            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync("anonaccess");

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task ValidSession()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/adminaccess");
            requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("test_user", Roles.Admin)}");
            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.Multiple(() =>
            {
                Assert.That(resp.Headers.Contains("Set-Cookie"));
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public async Task MissingCookie()
        {
            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync("adminaccess");

            Assert.Multiple(() =>
            {
                Assert.That(resp.Headers.Contains("Set-Cookie"), Is.False);
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            });
        }

        [Test]
        public async Task InvalidToken()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/adminaccess");
            requestBuilder.AddHeader("Cookie", "warehouse-session=invalid");
            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.Multiple(() =>
            {
                Assert.That(resp.Headers.Contains("Set-Cookie"), Is.False);
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            });
        }

        [Test]
        public async Task ExpiredToken()
        {
            _appFactory.Now = DateTime.UtcNow.AddDays(-7);

            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/adminaccess");
            requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("test_user", Roles.Admin)}");
            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.Multiple(() =>
            {
                Assert.That(resp.Headers.Contains("Set-Cookie"), Is.False);
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            });
        }

        [Test]
        public async Task MissingRole()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/adminaccess");
            requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("test_user", Roles.User)}");
            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.Multiple(() =>
            {
                Assert.That(resp.Headers.Contains("Set-Cookie"));
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            });
        }
    }
}
