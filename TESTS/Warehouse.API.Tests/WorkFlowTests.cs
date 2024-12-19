using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    using DAL;

    [TestFixture]
    internal sealed class WorkFlowTests
    {
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
                        new CreateGroupParam { Name = "Admins", Roles = ["Admin", "User"] },
                        new CreateGroupParam { Name = "Users", Roles = ["User"] }
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
        public async Task TestLoginAndQueryItem()
        {
            using HttpClient client = _appFactory.CreateClient();

            string sessionCookie;

            RequestBuilder requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/login");
            requestBuilder.AddHeader("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"root:{_appFactory.RootPw}"))}");
            using (HttpResponseMessage resp = await requestBuilder.GetAsync())
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

                sessionCookie = resp.Headers.GetValues("Set-Cookie").Single().Split(";")[0];
            }

            requestBuilder = _appFactory.Server.CreateRequest("http://localhost/api/v1/product/1986");
            requestBuilder.AddHeader("Cookie", sessionCookie);

            using (HttpResponseMessage resp = await requestBuilder.GetAsync())
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }
    }
}