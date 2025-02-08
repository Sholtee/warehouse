/********************************************************************************
* SwaggerTests.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Registrations;

    [ApiController]
    public sealed class SwaggerExposedController : ControllerBase
    {
        [HttpGet("/test")]
        public IActionResult Endpoint(string param) => Ok();
    }

    [TestFixture]
    internal sealed class SwaggerTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            public IConfiguration Configuration { get; set; } = null!;

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureAppConfiguration(config => Configuration = config.Build())
                .ConfigureTestServices
                (
                    services =>
                    {
                        services           
                            .AddMvcCore()
                            .AddApiExplorer()
                            .AddApplicationPart(typeof(SwaggerExposedController).Assembly)
                            .AddControllersAsServices();

                        services.AddSwagger(Configuration);
                    }
                )
                .Configure
                (
                    static app => app
                        .UseSwagger()
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
        public async Task TestSwaggerEndpoint()
        {
            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("swagger/v1/swagger.json");

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            IDictionary? schema = await resp.Content.ReadFromJsonAsync<IDictionary>();
            Assert.That(schema, Does.ContainKey("openapi"));
        }
    }
}
