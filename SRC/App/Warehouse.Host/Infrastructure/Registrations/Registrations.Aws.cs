/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon;
using Amazon.SecretsManager;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Helpers;

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
                .TryAddAWSService<IAmazonSecretsManager>();
        }
    }
}
