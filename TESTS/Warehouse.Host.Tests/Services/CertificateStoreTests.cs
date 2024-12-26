/********************************************************************************
* CertificateStoreTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    [TestFixture]
    internal sealed class CertificateStoreTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private Mock<IOptions<JsonOptions>> _mockJsonOptions = null!;
        private Mock<Func<string, string, X509Certificate2>> _mockCreateCert = null!;
        private CertificateStore _certificateStore = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockConfiguration = new(MockBehavior.Strict);
            _mockSecretsManager = new(MockBehavior.Strict);
            _mockJsonOptions = new(MockBehavior.Strict);    
            _mockCreateCert = new(MockBehavior.Strict);

            _certificateStore = new
            (
                _mockConfiguration.Object,
                _mockSecretsManager.Object,
                _mockJsonOptions.Object
            )
            {
                CreateFromPem = _mockCreateCert.Object
            };
        }

        [Test]
        public async Task GetCertificate_ShouldReturnTheCertificate()
        {
            Mock<IConfigurationSection> mockEnv = new(MockBehavior.Strict);
            mockEnv
                .SetupGet(s => s.Value)
                .Returns("local");
            mockEnv
                .SetupGet(s => s.Path)
                .Returns((string) null!);

            _mockConfiguration
                .Setup(c => c.GetSection("ASPNETCORE_ENVIRONMENT"))
                .Returns(mockEnv.Object);

            _mockJsonOptions
                .SetupGet(o => o.Value)
                .Returns(new JsonOptions());

            _mockSecretsManager
                .Setup
                (
                    t => t.GetSecretValueAsync
                    (
                        It.Is<GetSecretValueRequest>
                        (
                            r => r.SecretId == "local-warehouse-app-certificate"
                        ),
                        default
                    )
                )
                .ReturnsAsync
                (
                    new GetSecretValueResponse
                    {
                        SecretString = "{\"privateKey\": \"privateKey\", \"certificate\": \"certificate\"}"
                    }
                );


            X509Certificate2 cert = new();
            _mockCreateCert
                .Setup(c => c.Invoke("certificate", "privateKey"))
                .Returns(cert);

            Assert.That(await _certificateStore.GetCertificateAsync("warehouse-app-certificate"), Is.EqualTo(cert));

            _mockCreateCert.Verify(c => c.Invoke(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockSecretsManager.Verify(t => t.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Once);
        }
    }
}
