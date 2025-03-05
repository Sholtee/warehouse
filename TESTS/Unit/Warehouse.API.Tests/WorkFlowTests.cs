/********************************************************************************
* WorkflowTests.cs                                                              *
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using ServiceStack.OrmLite;

namespace Warehouse.API.Tests
{
    using Core.Auth;
    using DAL;
    using Host;
    using Warehouse.Tests.Core;

    [TestFixture, NonParallelizable, RequireRedis]
    internal sealed class WorkFlowTests
    {
        #region Private
        private sealed class TestHostFactory : WebApplicationFactory<Program>
        {
            private readonly SqliteConnection _connection;

            public TestHostFactory()
            {
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

                StringBuilder schemaSetup = new();
                schemaSetup.AppendLine
                (
                    Schema.Dump()
                );
                schemaSetup.AppendLine
                (
                    Schema.Dump
                    (
                        new CreateGroupParam { Name = "Admins", Roles = Roles.Admin | Roles.User },
                        new CreateGroupParam { Name = "Users", Roles = Roles.User }
                    )
                );

                _connection.CreateFunction("UUID", Guid.NewGuid);
                _connection.CreateAggregate<int, int>("BIT_OR", static (accu, curr) => accu | curr);

                _connection.ExecuteNonQuery(schemaSetup.ToString());             
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                    _connection.Dispose();
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
                .UseEnvironment("local")
                .ConfigureTestServices(services =>
                {
                    Mock<IAmazonSecretsManager> mockSecretsManager = new(MockBehavior.Strict);
                    mockSecretsManager
                        .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-warehouse-root-user-password"), default))
                        .ReturnsAsync(new GetSecretValueResponse { SecretString = "password" });

                    Mock<IAmazonSecurityTokenService> mockSts = new(MockBehavior.Strict);
                    mockSts
                        .Setup(s => s.GetCallerIdentityAsync(It.IsAny<GetCallerIdentityRequest>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new GetCallerIdentityResponse());
                    mockSts
                        .Setup(s => s.Dispose());

                    services.AddSingleton(mockSts.Object);
                    services.AddSingleton(mockSecretsManager.Object);
                    services.AddSingleton<IDbConnection>(_connection);
                    services.AddSingleton<IOrmLiteDialectProvider>(SqliteDialect.Provider);
                })
                .ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection
                (
                    new Dictionary<string, string?>
                    {
                        ["WAREHOUSE_REDIS_CONNECTION"] = "localhost:6379"
                    }
                ));
        }

        private TestHostFactory _appFactory = null!;

        private async Task<string> GetSessionCookie()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"root:password"))}");

            using HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            return resp.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
        }

        private static void RunParallel(int parallelism, Func<Task> taskFactory) => Assert.DoesNotThrow
        (
            Task.WhenAll
            (
                Enumerable.Repeat(0, parallelism).Select(_ => taskFactory())
            ).Wait
        );
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
        public void TestLoginLogout([Values(1, 2, 5, 10)] int parallelism) => RunParallel(parallelism, async () =>
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");

            await CheckUnauthorized();

            requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"unknown:password"))}");

            await CheckUnauthorized();

            requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"root:badpw"))}");

            await CheckUnauthorized();

            requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"root:password"))}");

            string session = await CheckCookieExpiration(Is.GreaterThan(DateTime.Now));

            requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/logout");
            requestBuilder.AddHeader("Cookie", session);

            await CheckCookieExpiration(Is.LessThan(DateTime.Now));

            async Task<string> CheckCookieExpiration(IConstraint constraint)
            {
                using HttpResponseMessage resp = await requestBuilder.GetAsync();

                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

                string[] cookieData = resp.Headers.GetValues("Set-Cookie").Single().Split(";");

                Assert.That(cookieData[0].Trim(), Does.StartWith("warehouse-session"));
                Assert.That(cookieData[1].Trim(), Does.StartWith("expires"));
                Assert.That(DateTime.Parse(cookieData[1].Split("=")[1], null), constraint);

                return cookieData[0];
            }

            async Task CheckUnauthorized()
            {
                using HttpResponseMessage resp = await requestBuilder.GetAsync();

                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That(resp.Headers.GetValues("WWW-Authenticate").Single(), Is.EqualTo("Basic"));
            }
        });

        [Test]
        public void TestLoginAndGetProduct([Values(1, 2, 5, 10)] int parallelism) => RunParallel(parallelism, async () =>
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest($"http://localhost/api/v1/product/{Guid.Empty}");
            requestBuilder.AddHeader("Cookie", await GetSessionCookie());

            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });

        [Test]
        public void TestLoginAndQueryProducts([Values(1, 2, 5, 10)] int parallelism) => RunParallel(parallelism, async () =>
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/products/");
            requestBuilder.AddHeader("Cookie", await GetSessionCookie());
            requestBuilder.And
            (
                msg => msg.Content = new StringContent
                (
                    """
                {
                    // (Brand == "Samsung" && "Price" < 1000) || (Brand == "Sony" && "Price" < 1500)
                    "filter": {
                        "block": {
                            "string": {
                                "property": "Brand",
                                "comparison": "equals",
                                "value": "Samsung"
                            },
                            "and": {
                                "decimal": {
                                    "property": "Price",
                                    "comparison": "lessThan",
                                    "value": 1000
                                }
                            }
                        },
                        "or": {
                            "block": {
                                "string": {
                                    "property": "Brand",
                                    "comparison": "equals",
                                    "value": "Sony"
                                },
                                "and": {
                                    "decimal": {
                                        "property": "Price",
                                        "comparison": "lessThan",
                                        "value": 1500
                                    }
                                }
                            }
                        }
                    },
                    "sortBy": {
                        "properties": [
                            {"property": "Brand", "kind": "ascending"},
                            {"property": "Name", "kind": "ascending"}
                        ]
                    },
                    "page": {
                        "skip": 3,
                        "size": 10
                    }
                }
                """,
                    Encoding.UTF8,
                    "application/json"
                )
            );

            HttpResponseMessage resp = await requestBuilder.PostAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(resp.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        });
    }
}
