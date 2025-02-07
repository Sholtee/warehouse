/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon.SecretsManager;
using Amazon.SecurityToken;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    internal static partial class Registrations
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services) => services
            .TryAddAWSService<IAmazonSecurityTokenService>() // required for the health checks
            .TryAddAWSService<IAmazonSecretsManager>();
    }
}
