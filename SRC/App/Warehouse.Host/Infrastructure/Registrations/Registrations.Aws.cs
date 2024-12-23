/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon.CertificateManager;
using Amazon.ResourceGroupsTaggingAPI;
using Amazon.SecretsManager;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    internal static partial class Registrations
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services) => services
            .TryAddAWSService<IAmazonCertificateManager>()
            .TryAddAWSService<IAmazonResourceGroupsTaggingAPI>()
            .TryAddAWSService<IAmazonSecretsManager>();
    }
}
