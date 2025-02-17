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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Amazon.Runtime;
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
    using Registrations;
    using Warehouse.Tests.Core;

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

    [TestFixture, NonParallelizable, RequireRedis, RequireLocalStack("sts")]
    internal sealed class ProfilerTests
    {
        #region Private
        private sealed class TestHostFactory : WebApplicationFactory<Warehouse.Tests.Host.Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureAppConfiguration
                (
                    static config => config
                        .AddJsonFile("appsettings.json")
                        .AddInMemoryCollection
                        (
                            new Dictionary<string, string?>
                            {
                                ["WAREHOUSE_REDIS_CONNECTION"] = "localhost:6379"
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
                        .AddAwsServices((_, opts) =>
                        {
                            opts.Credentials = new BasicAWSCredentials("local", "local");
                            opts.DefaultClientConfig.ServiceURL = "http://localhost:4566";
                        })
                        .AddStatelessAuthentication()
                        .AddRedis()
                        .AddProfiler();

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

            ITokenManager tokenManager = scope.ServiceProvider.GetRequiredService<ITokenManager>();
            return await tokenManager.CreateTokenAsync(user, role);
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
        public async Task OnlyRootCanAccessTheProfilerResults([Values(1, 2, 5, 10)] int sessions)
        {
            //
            // Do some work
            //

            string[] profilerIds = await Task.WhenAll(Enumerable.Repeat(0, sessions).Select(_ => DoWork()));
            Assert.That(profilerIds, Has.Length.EqualTo(profilerIds.Distinct().Count()));

            foreach (string profilerId in profilerIds)
            {
                string resultsUri = $"http://localhost/profiler/results?id={profilerId}";

                //
                // Check that no one can access the profiling results except the root
                //

                HttpResponseMessage resp;

                using (HttpClient client = _appFactory.CreateClient())
                {
                    resp = await client.GetAsync(resultsUri);
                    Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                }

                RequestBuilder requestBuilder = _appFactory.Server.CreateRequest(resultsUri);
                requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("test_user", Roles.Admin, DateTimeOffset.Now.AddMinutes(5))}");
                resp = await requestBuilder.GetAsync();
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

                requestBuilder = _appFactory.Server.CreateRequest(resultsUri);
                requestBuilder.AddHeader("Cookie", $"warehouse-session={await CreateToken("root", Roles.Admin, DateTimeOffset.Now.AddMinutes(5))}");
                resp = await requestBuilder.GetAsync();
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }

            async Task<string> DoWork()
            {
                using HttpClient client = _appFactory.CreateClient();

                HttpResponseMessage resp = await client.GetAsync("foo");
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                return JsonSerializer.Deserialize<string[]>(resp.Headers.GetValues("X-MiniProfiler-Ids").Single())!.Single();
            }
        }
    }
}
