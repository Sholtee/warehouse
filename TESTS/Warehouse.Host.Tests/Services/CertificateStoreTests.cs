/********************************************************************************
* CertificateStoreTests.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.ResourceGroupsTaggingAPI;
using Amazon.ResourceGroupsTaggingAPI.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Services.Tests
{
    using Core.Abstractions;

    [TestFixture]
    internal sealed class CertificateStoreTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IAmazonResourceGroupsTaggingAPI> _mockTaggingApi = null!;
        private Mock<IAmazonCertificateManager> _mockCertificateManager = null!;
        private Mock<IPasswordGenerator> _mockPasswordGenerator = null!;
        private Mock<ILogger<CertificateStore>> _mockLogger = null!;
        private Mock<Func<string, string, string, X509Certificate2>> _mockCreateCert = null!;
        private CertificateStore _certificateStore = null!;

        [SetUp]
        public void SetupTest()
        {
            _mockConfiguration = new(MockBehavior.Strict);
            _mockTaggingApi = new(MockBehavior.Strict);
            _mockCertificateManager = new(MockBehavior.Strict);
            _mockPasswordGenerator = new(MockBehavior.Strict);
            _mockLogger = new(MockBehavior.Loose);    
            _mockCreateCert = new(MockBehavior.Strict);

            _certificateStore = new
            (
                _mockConfiguration.Object,
                _mockTaggingApi.Object,
                _mockCertificateManager.Object,
                _mockPasswordGenerator.Object,
                _mockLogger.Object
            )
            {
                CreateFromEncryptedPem = _mockCreateCert.Object
            };
        }

        [Test]
        public async Task GetCertificate_ShouldReturnTheCertificateByTag()
        {
            const string certArn = "arn:aws:acm:us-east-1:000000000000:certificate/a5aa491d-764e-4cf6-922b-e639e69e2ddb";

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

            _mockTaggingApi
                .Setup
                (
                    t => t.GetResourcesAsync
                    (
                        It.Is<GetResourcesRequest>
                        (
                            r => r.TagFilters.Count(f => f.Key == "Project" && f.Values.Contains("local-warehouse-app")) == 1
                        ),
                        default
                    )
                )
                .ReturnsAsync
                (
                    new GetResourcesResponse
                    {
                        ResourceTagMappingList =
                        [
                            new ResourceTagMapping
                            {
                                ResourceARN = certArn
                            }
                        ]
                    }
                );

            _mockPasswordGenerator
                .Setup(p => p.Generate(20))
                .Returns("password");

            _mockCertificateManager
                .Setup
                (
                    c => c.ExportCertificateAsync
                    (
                        It.Is<ExportCertificateRequest>
                        (
                            r => r.CertificateArn == certArn && Encoding.UTF8.GetString(r.Passphrase.ToArray()) == "password"
                        ),
                        default
                    )
                )
                .ReturnsAsync
                (
                    new ExportCertificateResponse
                    {
                        Certificate = "certificate",
                        PrivateKey = "privateKey"
                    }
                );

            X509Certificate2 cert = new();
            _mockCreateCert
                .Setup(c => c.Invoke("certificate", "privateKey", "password"))
                .Returns(cert);

            Assert.That(await _certificateStore.GetCertificateAsync(null), Is.EqualTo(cert));

            _mockCreateCert.Verify(c => c.Invoke(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockCertificateManager.Verify(c => c.ExportCertificateAsync(It.IsAny<ExportCertificateRequest>(), default), Times.Once);
            _mockPasswordGenerator.Verify(p => p.Generate(It.IsAny<int>()), Times.Once);
            _mockTaggingApi.Verify(t => t.GetResourcesAsync(It.IsAny<GetResourcesRequest>(), default), Times.Once);
        }
    }
}
