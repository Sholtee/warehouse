/********************************************************************************
* UnhandledExceptionFilterTests.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
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
using NUnit.Framework.Constraints;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Core.Exceptions;
    using Filters;

    [ApiController]
    public sealed class ErrorTestController : ControllerBase
    {
        [HttpGet("badrequest")]
        public IActionResult BadRequestError() => throw new BadRequestException { DeveloperMessage = "dev message" };

        [HttpGet("notfound")]
        public IActionResult NoFoundError() => throw new NotFoundException();

        [HttpGet("internalerror")]
        public IActionResult InternalError() => throw new Exception("ooops");

        [HttpGet("modelerror/{id}")]
        public IActionResult ModelError(int id) => Ok();
    }


    [TestFixture]
    internal sealed class UnhandledExceptionFilterTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices
                (
                    services => services
                        .AddMvcCore(static options =>
                        {
                            options.Filters.Add<UnhandledExceptionFilter>();
                            options.Filters.Add<ValidateModelStateFilter>();
                        })
                        .ConfigureApiBehaviorOptions(static options => options.SuppressModelStateInvalidFilter = true)
                        .AddApplicationPart(typeof(AuthTestController).Assembly)
                        .AddControllersAsServices()
                );
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

        private async Task DoTest(string endpoint, HttpStatusCode expectedStatus, string expectedTitle, IResolveConstraint devMessageConstraint, string? expectedErrors = null)
        {
            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync(endpoint);

            Assert.That(resp.StatusCode, Is.EqualTo(expectedStatus));

            ErrorDetails? details = await resp.Content.ReadFromJsonAsync<ErrorDetails>();

            Assert.Multiple(() =>
            {
                Assert.That(details, Is.Not.Null);
                Assert.That(details!.Status, Is.EqualTo((int) expectedStatus));
                Assert.That(details.Title, Is.EqualTo(expectedTitle));
                Assert.That(details.TraceId, Is.Not.Null);
                Assert.That(details.DeveloperMessage?.ToString(), devMessageConstraint);
                Assert.That(details.Errors?.ToString(), Is.EqualTo(expectedErrors));
            });
        }

        [Test]
        public Task BadRequest() => DoTest("badrequest", HttpStatusCode.BadRequest, "Bad Request", Is.EqualTo("dev message"));

        [Test]
        public Task NotFound() => DoTest("notfound", HttpStatusCode.NotFound, "Not Found", Is.Null);

        [Test]
        public Task InternalError() => DoTest("internalerror", HttpStatusCode.InternalServerError, "Internal Server Error", Does.Contain("ooops"));

        [Test]
        public Task ModelError() => DoTest("modelerror/invalid", HttpStatusCode.BadRequest, "Bad Request", Is.Null, "{\"id\":[\"The value 'invalid' is not valid.\"]}");
    }
}
