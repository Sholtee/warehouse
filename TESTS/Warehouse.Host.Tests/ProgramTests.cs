using System;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;


namespace Warehouse.Host.Tests
{
    using Core.Abstractions;

    [TestFixture]
    internal sealed class ProgramTests
    {
        [Test]
        public void KestrelConfiguratorTest()
        {
            Mock<IConfiguration> mockConfiguration = new(MockBehavior.Strict);

            Mock<IConfigurationSection> mockPrefix = new(MockBehavior.Strict);
            mockPrefix
                .SetupGet(s => s.Value)
                .Returns("local");
            mockPrefix
                .SetupGet(s => s.Path)
                .Returns((string) null!);

            mockConfiguration
                .Setup(c => c.GetSection("Prefix"))
                .Returns(mockPrefix.Object);

            Mock<IConfigurationSection> mockPort = new(MockBehavior.Strict);
            mockPort
                .SetupGet(s => s.Value)
                .Returns("1986");
            mockPort
                .SetupGet(s => s.Path)
                .Returns((string) null!);

            mockConfiguration
                .Setup(c => c.GetSection("ApiPort"))
                .Returns(mockPort.Object);

            Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
            mockServiceProvider
                .Setup(s => s.GetService(typeof(IConfiguration)))
                .Returns(mockConfiguration.Object);

            X509Certificate2 cretedCert = new();
            Mock<IX509CertificateFactory> mockX509CertificateFactory = new(MockBehavior.Strict);
            mockX509CertificateFactory
                .Setup(f => f.CreateFromPem("certificate", "privateKey"))
                .Returns(cretedCert);
            mockServiceProvider
                .Setup(s => s.GetService(typeof(IX509CertificateFactory)))
                .Returns(mockX509CertificateFactory.Object);

            Mock<IAmazonSecretsManager> mockSecretsManager = new(MockBehavior.Strict);
            mockSecretsManager
                .Setup(s => s.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == "local-api-certificate"), default))
                .ReturnsAsync
                (
                    new GetSecretValueResponse
                    {
                        SecretString = JsonSerializer.Serialize(new { certificate = "certificate", privateKey = "privateKey" })
                    }
                );
            mockServiceProvider
                .Setup(s => s.GetService(typeof(IAmazonSecretsManager)))
                .Returns(mockSecretsManager.Object);

            Type configurationService = typeof(KestrelServerOptions).Assembly.GetType
            (
                "Microsoft.AspNetCore.Server.Kestrel.Core.IHttpsConfigurationService",  // this is an internal interface
                throwOnError: true
            )!;

            //
            // It would be tough to mock the internal interface so ust signal we reached to the configuration phase
            //

            Exception configPhaseReached = new("Configuration phase reached");
            mockServiceProvider
                .Setup(s => s.GetService(configurationService))
                .Throws(configPhaseReached);

            WebHostBuilderContext context = new()
            {
                Configuration = mockConfiguration.Object
            };

            KestrelServerOptions options = new()
            {
                ApplicationServices = mockServiceProvider.Object
            };

            Exception ex = Assert.Throws<Exception>(() => Program.UsingHttps(context, options));
            Assert.That(ex, Is.SameAs(configPhaseReached));

            mockSecretsManager.Verify(s => s.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), default), Times.Once);
            mockX509CertificateFactory.Verify(f => f.CreateFromPem(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
