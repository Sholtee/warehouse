/********************************************************************************
* RateLimitingTests.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Middlewares;
    using Registrations;

    [ApiController, EnableRateLimiting("fixed")]
    public sealed class FixedRateLimitingTestController : ControllerBase
    {
        [HttpGet("fixed")]
        public IActionResult Fixed() => Ok();
    }

    [TestFixture]
    internal sealed class RateLimitingTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices
                (
                    static services =>
                    {
                        Dictionary<string, string?> rateLimitConfig = new()
                        {
                            {"RateLimiting:Fixed:PermitLimit", "1" },
                            {"RateLimiting:Fixed:Window", "00:01:00" }
                        };

                        services
                            .AddExceptionHandler<UnhandledExceptionHandler>()
                            .AddRateLimiter(new ConfigurationBuilder().AddInMemoryCollection(rateLimitConfig).Build())
                            .AddMvcCore()
                            .AddApplicationPart(typeof(FixedRateLimitingTestController).Assembly)
                            .AddControllersAsServices();
                    }
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
        }
    }
}
