/********************************************************************************
* Profiler.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using Amazon.SecretsManager.Model;
using Amazon.SecretsManager;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Moq;
using NUnit.Framework;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace Warehouse.Host.Infrastructure.Tests
{
    using Core.Abstractions;
    using Core.Auth;
    using Host.Tests;
    using Registrations;
    using System.Net.Http;
    using System.Net;

    [ApiController]
    public sealed class ProfiledController(IDbConnection conn, IAmazonSecurityTokenService sts) : ControllerBase
    {
        [HttpGet("foo")]
        public async Task<IActionResult> Foo()
        {
            await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest());

            await conn.SqlScalarAsync<int>("SELECT 1");

            return Ok();
        }
    }

    [TestFixture, RequireRedis, RequireLocalStack("sts")]
    internal sealed class ProfilerTests
    {
        #region Private
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            public IConfiguration Configuration { get; set; } = null!;

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureAppConfiguration
                (
                    config => Configuration = config
                        .AddJsonFile("appsettings.json")
                        .AddInMemoryCollection
                        (
                            new Dictionary<string, string?>
                            {
                                ["AWS_REGION"] = "local",
                                ["AWS_ACCESS_KEY_ID"] = "local",
                                ["AWS_SECRET_ACCESS_KEY"] = "local",
                                ["AWS_ENDPOINT_URL"] = "http://localhost:4566",

                                ["WAREHOUSE_REDIS_ENDPOINT"] = "localhost:6379"
                            }
                        )
                        .Build()
                )
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

                    services.AddSingleton(mockSecretsManager.Object);

                    OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

                    services.AddScoped<IDbConnection>(_ =>
                    {
                        IDbConnection conn = new SqliteConnection("DataSource=:memory:");
                        conn.Open();
                        return conn;
                    });

                    services
                        .AddAwsServices()
                        .AddSessionCookieAuthentication()
                        .AddProfiler(Configuration);

                    services
                        .AddMvc()
                        .AddApplicationPart(typeof(ProfiledController).Assembly)
                        .AddControllersAsServices();
                })
                .Configure
                (
                    static app => app
                        .UseExceptionHandler(static _ => { })
                        .UseRouting()
                        .UseAuthorization()
                        .UseProfiling()
                        .UseEndpoints(static endpoints => endpoints.MapControllers())
                );
        }

        private TestHostFactory _appFactory = null!;

        private async Task<string> CreateToken(string user, Roles role, DateTimeOffset expiration)
        {
            using IServiceScope scope = _appFactory.Services.CreateScope();

            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            return await jwtService.CreateTokenAsync(user, role, expiration);
        }
        #endregion

        [SetUp]
        public void SetupTest() => _appFactory = new TestHostFactory();

        [TearDown]
        public void TearDownTest()
        {
            _appFactory?.Dispose();
            _appFactory = null!;
        }

        [Test]
        public async Task OnlyRootCanAccessTheProfilerResults()
        {
            //
            // Do some work
            //

            using HttpClient client = _appFactory.CreateClient();

            HttpResponseMessage resp = await client.GetAsync("foo");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}
