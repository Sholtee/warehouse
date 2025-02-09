/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecurityToken;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Profiling;

    internal static partial class Registrations
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services)
        {
            //
            // ProfiledHttpClient does nothing if the profiler was not started
            // (MiniProfiler.Current == null)
            //

            AWSConfigs.HttpClientFactory ??= new ProfiledHttpClientFactory();

            return services
                .TryAddAWSService<IAmazonSecurityTokenService>() // required for the health checks
                .TryAddAWSService<IAmazonSecretsManager>();
        }
    }
}
