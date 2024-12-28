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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    [TestFixture]
    internal sealed class CertificateStoreTests
    {
        private Mock<IAmazonSecretsManager> _mockSecretsManager = null!;
        private Mock<IHostEnvironment> _mockHostEnvironment = null!;
        private Mock<IOptions<JsonOptions>> _mockJsonOptions = null!;
        private Mock<Func<string, string, X509Certificate2>> _mockCreateCert = null!;
        private CertificateStore _certificateStore = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockHostEnvironment = new(MockBehavior.Strict);
            _mockHostEnvironment
                .SetupGet(e => e.EnvironmentName)
                .Returns("local");

            _mockSecretsManager = new(MockBehavior.Strict);
            _mockJsonOptions = new(MockBehavior.Strict);    
            _mockCreateCert = new(MockBehavior.Strict);

            _certificateStore = new
            (
                _mockHostEnvironment.Object,
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
