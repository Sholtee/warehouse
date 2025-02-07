/********************************************************************************
* HealthCheckTests.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using NUnit.Framework;
using ServiceStack.OrmLite;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Registrations;

    [TestFixture]
    internal sealed class HealthCheckTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            public IDbConnection Connection { get; set; } = null!;

            public IAmazonSecurityTokenService Sts { get; set; } = null!;

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => Connection);
                    services.AddSingleton(_ => Sts);

                    services
                        .AddHealthCheck()
                        .AddMvcCore();
                })
                .Configure
                (
                    static app => app
                        .UseExceptionHandler(static _ => { })
                        .UseHealthCheck()
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
        public async Task TestHealthCheckOk()
        {
            _appFactory.Connection = new SqliteConnection("DataSource=:memory:");
            _appFactory.Connection.Open();

            Mock<IAmazonSecurityTokenService> mockSts = new(MockBehavior.Strict);
            mockSts
                .Setup(s => s.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCallerIdentityResponse());
            mockSts
                .Setup(s => s.Dispose());

            _appFactory.Sts = mockSts.Object;

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("/healthcheck");

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(resp.Content.Headers.ContentType?.ToString(), Does.StartWith("application/json"));

                IDictionary<string, string>? content = await resp.Content.ReadFromJsonAsync<IDictionary<string, string>>();

                Assert.That(content?.Count, Is.EqualTo(1));
                Assert.That(content!["status"], Is.EqualTo("Healthy"));
            });
        }

        [Test]
        public async Task TestHealthCheckFailed()
        {
            Mock<IDbConnection> mockConnection = new Mock<IDbConnection>(MockBehavior.Strict);
            mockConnection
                .Setup(s => s.CreateCommand())
                .Throws(new Exception("DB exception"));
            mockConnection
                .Setup(s => s.Dispose());

            _appFactory.Connection = mockConnection.Object;

            Mock<IAmazonSecurityTokenService> mockSts = new(MockBehavior.Strict);
            mockSts
                .Setup(s => s.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("STS exception"));
            mockSts
                .Setup(s => s.Dispose());

            _appFactory.Sts = mockSts.Object;

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("/healthcheck");

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(resp.Content.Headers.ContentType?.ToString(), Does.StartWith("application/json"));

                IDictionary<string, object>? content = await resp.Content.ReadFromJsonAsync<IDictionary<string, object>>();

                Assert.That(content?.Count, Is.EqualTo(2));
                Assert.That(content!["status"].ToString(), Is.EqualTo("Unhealthy"));
            });
        }
    }
}
