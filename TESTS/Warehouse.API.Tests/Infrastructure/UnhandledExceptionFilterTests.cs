using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

using Warehouse.Tests.Server;

namespace Warehouse.API.Infrastructure.Tests
{
    using Exceptions;
    using Filters;

    [ApiController]
    public sealed class ErrorTestController : ControllerBase
    {
        [HttpGet("badrequest")]
        public IActionResult BadRequestError() => throw new BadRequestException { DeveloperMessage = "dev message" };

        [HttpGet("notfound")]
        public IActionResult NoFoundError() => throw new NotFoundException();

        [HttpGet("unknown")]
        public IActionResult UnknownError() => throw new Exception();
    }


    [TestFixture]
    internal sealed class UnhandledExceptionFilterTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<TestHost>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureTestServices
                (
                    services => services
                        .AddMvcCore(static options =>
                        {
                            options.Filters.Add<UnhandledExceptionFilter>();
                            options.Filters.Add<ValidateModelStateFilter>();
                        })
                        .AddApplicationPart(typeof(AuthTestController).Assembly)
                        .AddControllersAsServices()
                );
            }
        }

        private TestHostFactory _appFactory = null!;

        [SetUp]
        public void SetupTest() => _appFactory = new TestHostFactory();

        [TearDown]
        public void TeardDownTest()
        {
            _appFactory?.Dispose();
            _appFactory = null!;
        }

        [Test]
        public async Task BadRequest()
        {
            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync("badrequest");

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            ErrorDetails? details = await resp.Content.ReadFromJsonAsync<ErrorDetails>();
            
            Assert.Multiple(() =>
            {
                Assert.That(details, Is.Not.Null);
                Assert.That(details!.Status, Is.EqualTo(400));
                Assert.That(details.Title, Is.EqualTo("Bad Request"));
                Assert.That(details.TraceId, Is.Not.Null);
                Assert.That(details.DeveloperMessage?.ToString(), Is.EqualTo("dev message"));
            });
        }
    }
}
