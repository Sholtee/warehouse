/********************************************************************************
* CertificateStore.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Amazon.CertificateManager;
using Amazon.CertificateManager.Model;
using Amazon.ResourceGroupsTaggingAPI.Model;
using Amazon.ResourceGroupsTaggingAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Extensions;

    internal sealed class CertificateStore
    (
        IConfiguration configuration,
        IAmazonResourceGroupsTaggingAPI taggingAPI,
        IAmazonCertificateManager certificateManager,
        IPasswordGenerator passwordGenerator,
        ILogger<CertificateStore> logger
    )
    {
        internal Func<string, string, string, X509Certificate2> CreateFromEncryptedPem { get; init; } = static (cert, privateKey, passphrase) => // to be mocked
            X509Certificate2.CreateFromEncryptedPem(cert, privateKey, passphrase);

        public async Task<X509Certificate2> GetCertificateAsync(string? certificateArn)
        {
            if (certificateArn is null)
            {
                //
                // This branch is for the local env only where we can't get the ARN from env var
                //

                logger.LogInformation("No certificate ARN provided, getting the master cert.");

                //
                // ACM doesn't support named certificates, so grab it by tag
                //

                GetResourcesResponse resources = await taggingAPI.GetResourcesAsync
                (
                    new GetResourcesRequest
                    {
                        TagFilters =
                        [
                            new TagFilter
                            {
                                Key = "Project",
                                Values =
                                [
                                    $"{configuration.GetRequiredValue<string>("ASPNETCORE_ENVIRONMENT")}-warehouse-app"
                                ]
                            }
                        ]
                    }
                );

                string[] appResources = [.. resources.ResourceTagMappingList.Select(static tm => tm.ResourceARN)];
                logger.LogInformation("Available app resources: {resources}", string.Join(", ", appResources));

                Regex matcher = new("^arn:aws:acm:\\S+:certificate\\/", RegexOptions.Compiled);

                certificateArn = appResources.Single(matcher.IsMatch);
                logger.LogInformation("Certificate ARN found: {arn}", certificateArn);
            }

            //
            // Grab the certificate
            //

            string passphrase = passwordGenerator.Generate(20);

            ExportCertificateResponse certificate = await certificateManager.ExportCertificateAsync
            (
                new ExportCertificateRequest
                {
                    CertificateArn = certificateArn,
                    Passphrase = new MemoryStream(Encoding.UTF8.GetBytes(passphrase))  // must be provided
                }
            );

            logger.LogInformation("Certificate retrieved");

            return CreateFromEncryptedPem(certificate.Certificate, certificate.PrivateKey, passphrase);
        }
    }
}
