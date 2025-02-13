/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecurityToken;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Extensions;
    using Profiling;

    internal static partial class Registrations
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services) => services
            .AddDefaultAWSOptions(static sp =>
            {
                IConfiguration configuration = sp.GetRequiredService<IConfiguration>();

                AWSOptions opts = new();

                //
                // ProfiledHttpClient does nothing if the profiler was not started
                // (MiniProfiler.Current == null)
                //

                opts.DefaultClientConfig.HttpClientFactory = new ProfiledHttpClientFactory();
                opts.Credentials = new BasicAWSCredentials
                (
                    //
                    // Always read the creds from config to prevent accidental AWS invocations
                    //

                    configuration.GetRequiredValue<string>("AWS_ACCESS_KEY_ID"),
                    configuration.GetRequiredValue<string>("AWS_SECRET_ACCESS_KEY")
                );

                string? serviceUrl = configuration.GetValue<string>("AWS_ENDPOINT_URL");
                if (!string.IsNullOrEmpty(serviceUrl))
                    opts.DefaultClientConfig.ServiceURL = serviceUrl;

                return opts;
            })
            .TryAddAWSService<IAmazonSecurityTokenService>() // required for the health checks
            .TryAddAWSService<IAmazonSecretsManager>();
    }
}
