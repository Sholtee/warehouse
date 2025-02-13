/********************************************************************************
* RateLimitingTests.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using NUnit.Framework;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Core.Abstractions;
    using Core.Attributes;
    using Core.Auth;
    using Middlewares;
    using Registrations;

    [ApiController, EnableRateLimiting("fixed")]
    public sealed class FixedRateLimitingTestController : ControllerBase
    {
        [HttpGet("fixed")]
        public IActionResult Fixed() => Ok();
    }

    [TestFixture]
    internal sealed class FixedWindowRateLimitingTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices
                (
                    static services => services
                        .AddExceptionHandler<UnhandledExceptionHandler>()
                        .AddRateLimiter()
                        .AddMvcCore()
                        .AddApplicationPart(typeof(FixedRateLimitingTestController).Assembly)
                        .AddControllersAsServices()
                )
                .ConfigureAppConfiguration
                (
                    static (context, bldr) => bldr.AddInMemoryCollection
                    (
                        new Dictionary<string, string?>
                        {
                            ["RateLimiting:Fixed:PermitLimit"] = "1",
                            ["RateLimiting:Fixed:Window"] = "00:00:02" 
                        }
                    )
                )
                .Configure
                (
                    static app => app 
                        .UseExceptionHandler(static _ => { })
                        .UseRouting()
                        .UseRateLimiter()
                        .UseEndpoints(static endpoints => endpoints.MapControllers())                        
                );
        }

        private TestHostFactory _appFactory = null!;

        [SetUp]
        public void SetupTest() => _appFactory = new TestHostFactory();

        [TearDown]
        public void TearDownTest()
        {
            _appFactory?.Dispose();
            _appFactory = null!;
        }

        [Test]
        public async Task FixedRateLimiting()
        {
            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync("fixed");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            resp = await client.GetAsync("fixed");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

            await Task.Delay(2500);

            resp = await client.GetAsync("fixed");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }

    [ApiController, Authorize, EnableRateLimiting("userBound")]
    public sealed class UserBoundRateLimitingTestsController : ControllerBase
    {
        [HttpGet("anon")]
        [AllowAnonymous]
        public IActionResult AnonAccess() => Ok();

        [HttpGet("admin")]
        [RequiredRoles(Roles.Admin)]
        public IActionResult AdminAccess() => Ok();
    }

    [TestFixture]
    internal sealed class UserBoundRateLimitingTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices(static services =>
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

                    services
                        .AddSingleton(mockSecretsManager.Object)
                        .AddExceptionHandler<UnhandledExceptionHandler>()
                        .AddRateLimiter();

                    services.AddSessionCookieAuthentication();

                    services
                        .AddMvc()
                        .AddApplicationPart(typeof(UserBoundRateLimitingTestsController).Assembly)
                        .AddControllersAsServices();
                })
                .ConfigureAppConfiguration
                (
                    static (context, bldr) => bldr.AddInMemoryCollection
                    (
                        new Dictionary<string, string?>
                        {
                            ["RateLimiting:UserBound:TokenLimit"] = "1",
                            ["RateLimiting:UserBound:TokensPerPeriod"] = "1",
                            ["RateLimiting:UserBound:ReplenishmentPeriod"] = "00:00:02",

                            ["RateLimiting:Anon:TokenLimit"] = "1",
                            ["RateLimiting:Anon:TokensPerPeriod"] = "1",
                            ["RateLimiting:Anon:ReplenishmentPeriod"] = "00:00:02"
                        }
                    )
                )
                .Configure
                (
                    static app => app
                        .UseExceptionHandler(static _ => { })
                        .UseRouting()
                        .UseAuthorization()
                        .UseRateLimiter()
                        .UseEndpoints(static endpoints => endpoints.MapControllers())
                );
        }

        private TestHostFactory _appFactory = null!;

        private async Task<string> CreateToken(string user, Roles role)
        {
            using IServiceScope scope = _appFactory.Services.CreateScope();

            ITokenManager jwtService = scope.ServiceProvider.GetRequiredService<ITokenManager>();
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

            HttpResponseMessage resp = await client.GetAsync("anon");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            resp = await client.GetAsync("anon");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

            await Task.Delay(2500);

            resp = await client.GetAsync("anon");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task AdminAccess()
        {
            HttpResponseMessage resp = await SendRequest();
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            resp = await SendRequest();
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

            await Task.Delay(2500);

            resp = await SendRequest();
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            async Task<HttpResponseMessage> SendRequest()
            {
                RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/admin");
                requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("test_user", Roles.Admin)}");
                return await requestBuilder.GetAsync();
            }
        }
    }
}
