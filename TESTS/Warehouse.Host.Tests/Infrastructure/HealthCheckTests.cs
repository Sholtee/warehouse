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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using StackExchange.Redis;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Dtos;
    using Registrations;

    [TestFixture]
    internal sealed class HealthCheckTests
    {
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            public IDbConnection DbConnection { get; set; } = null!;

            public IAmazonSecurityTokenService Sts { get; set; } = null!;

            public IConnectionMultiplexer RedisConnection { get; set; } = null!;

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => DbConnection);
                    services.AddSingleton(_ => Sts);
                    services.AddSingleton(_ => RedisConnection);

                    services
                        .AddHealthCheck()
                        .AddMvcCore();
                })
                .Configure
                (
                    static app => app.UseHealthCheck()
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
            _appFactory.DbConnection = new SqliteConnection("DataSource=:memory:");
            _appFactory.DbConnection.Open();

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

            Mock<IAmazonSecurityTokenService> mockSts = new(MockBehavior.Strict);
            mockSts
                .Setup(s => s.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCallerIdentityResponse());
            mockSts
                .Setup(s => s.Dispose());

            _appFactory.Sts = mockSts.Object;

            Mock<IDatabase> mockDb = new(MockBehavior.Strict);
            mockDb
                .Setup(d => d.PingAsync(CommandFlags.None))
                .ReturnsAsync(TimeSpan.Zero);

            Mock<IConnectionMultiplexer> mockRedisConnection = new(MockBehavior.Strict);
            mockRedisConnection
                .Setup(c => c.GetDatabase(-1, null))
                .Returns(mockDb.Object);
            mockRedisConnection
                .Setup(c => c.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            _appFactory.RedisConnection = mockRedisConnection.Object;

            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("/healthcheck");

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(resp.Content.Headers.ContentType?.ToString(), Does.StartWith("application/json"));

                HealthCheckResult? result = await resp.Content.ReadFromJsonAsync<HealthCheckResult>();
            
                Assert.That(result?.Status, Is.EqualTo("Healthy"));
                Assert.That(result!.Details, Is.Null);
            });
        }

        [Test]
        public async Task TestHealthCheckFailed()
        {
            Mock<IDbConnection> mockConnection = new(MockBehavior.Strict);
            mockConnection
                .Setup(s => s.CreateCommand())
                .Throws(new Exception("DB exception"));
            mockConnection
                .Setup(s => s.Dispose());

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

            _appFactory.DbConnection = mockConnection.Object;

            Mock<IAmazonSecurityTokenService> mockSts = new(MockBehavior.Strict);
            mockSts
                .Setup(s => s.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("STS exception"));
            mockSts
                .Setup(s => s.Dispose());

            _appFactory.Sts = mockSts.Object;

            Mock<IConnectionMultiplexer> mockRedisConnection = new(MockBehavior.Strict);
            mockRedisConnection
                .Setup(c => c.GetDatabase(-1, null))
                .Throws(new Exception("Redis exception"));
            mockRedisConnection
                .Setup(c => c.DisposeAsync())
                .Returns(ValueTask.CompletedTask);

            _appFactory.RedisConnection = mockRedisConnection.Object;
     
            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("/healthcheck");

            await Assert.MultipleAsync(async () =>
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(resp.Content.Headers.ContentType?.ToString(), Does.StartWith("application/json"));

                HealthCheckResult? result = await resp.Content.ReadFromJsonAsync<HealthCheckResult>();

                Assert.That(result?.Status, Is.EqualTo("Unhealthy"));
                Assert.That(result!.Details, Is.Not.Null);

                List<IDictionary<string, object>>? details = JsonSerializer.Deserialize<List<IDictionary<string, object>>>(result!.Details!.ToString()!);
                Assert.That(details!.Count, Is.EqualTo(3));
                Assert.That(details.Any(d => d["name"].ToString() == "AwsHealthCheck" && d["exception"].ToString() == "STS exception"));
                Assert.That(details.Any(d => d["name"].ToString() == "DbConnectionHealthCheck" && d["exception"].ToString() == "DB exception"));
                Assert.That(details.Any(d => d["name"].ToString() == "RedisHealthCheck" && d["exception"].ToString() == "Redis exception"));
            });
        }
    }
}
