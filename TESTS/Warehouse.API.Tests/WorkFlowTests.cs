using System;
using System.Collections;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using ServiceStack.OrmLite;

namespace Warehouse.API.Tests
{
    using Core.Auth;
    using DAL;
    using Host;

    [TestFixture]
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
                _connection.ExecuteNonQuery(schemaSetup.ToString());
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                    _connection.Dispose();
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.ConfigureTestServices(services =>
                {
                    Mock<IAmazonSecretsManager> mockSecretsManager = new(MockBehavior.Strict);
                    mockSecretsManager
                        .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-jwt-secret-key"), default))
                        .ReturnsAsync(new GetSecretValueResponse { SecretString = "very-very-very-very-very-secure-secret-key" });
                    mockSecretsManager
                        .Setup(s => s.CreateSecretAsync(It.Is<CreateSecretRequest>(r => r.Name == "local-root-user-creds"), default))
                        .Returns<CreateSecretRequest, CancellationToken>((r, t) =>
                        {
                            RootPw = r.SecretString;
                            return Task.FromResult<CreateSecretResponse>(null!);
                        });

                    services.AddSingleton(mockSecretsManager.Object);
                    services.AddSingleton<IDbConnection>(_connection);
                    services.AddSingleton<IOrmLiteDialectProvider>(SqliteDialect.Provider);
                });
            }

            public string RootPw { get; internal set; } = null!;
        }

        private TestHostFactory _appFactory = null!;

        private async Task<string> GetSessionCookie()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"root:{_appFactory.RootPw}"))}");

            using HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            return resp.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
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
        public async Task TestHealthCheck()
        {
            using HttpClient client = _appFactory.CreateClient();
            using HttpResponseMessage resp = await client.GetAsync("api/v1/healthcheck");

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        }

        [Test]
        public async Task TestLoginAndGetItem()
        {
            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest($"http://localhost/api/v1/product/{Guid.Empty}");
            requestBuilder.AddHeader("Cookie", await GetSessionCookie());

            HttpResponseMessage resp = await requestBuilder.GetAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task TestLoginAndQueryItems()
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
                            "skipPages": 3,
                            "pageSize": 10
                        }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"
                )
            );

            HttpResponseMessage resp = await requestBuilder.PostAsync();

            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
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
