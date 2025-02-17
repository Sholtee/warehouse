/********************************************************************************
* Registrations.Aws.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using Amazon.SecurityToken;
using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Profiling;

    internal static partial class Registrations
    {
        public static IServiceCollection AddAwsServices(this IServiceCollection services, Action<IServiceProvider, AWSOptions>? tweakOptions = null) => services
            .AddDefaultAWSOptions(sp =>
            {
                AWSOptions opts = new();

                //
                // ProfiledHttpClient does nothing if the profiler was not started
                // (MiniProfiler.Current == null)
                //

                opts.DefaultClientConfig.HttpClientFactory = new ProfiledHttpClientFactory();

                tweakOptions?.Invoke(sp, opts);

                return opts;
            })
            .TryAddAWSService<IAmazonSecurityTokenService>() // required for the health checks
            .TryAddAWSService<IAmazonSecretsManager>();
    }
}
