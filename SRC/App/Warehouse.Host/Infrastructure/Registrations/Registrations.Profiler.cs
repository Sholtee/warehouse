/********************************************************************************
* Registrations.Profiler.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Security.Principal;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Profiling;
using StackExchange.Profiling.Storage;
using StackExchange.Redis;

namespace Warehouse.Host.Infrastructure.Registrations
{  
    using Core.Abstractions;
    using Core.Auth;
    using Core.Extensions;
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddProfiler(this IServiceCollection services)
        {
            services.TryAddScoped<IProfiler>(static _ => new Profiler(MiniProfiler.Current!));
            services.AddMiniProfiler();
            services.AddOptions<MiniProfilerOptions, IConfiguration, IConnectionMultiplexer>(static (options, configuration, redisConnection) =>
            {
                IConfigurationSection profilerConfiguration = configuration.GetSection("MiniProfiler");
                if (!profilerConfiguration.Exists())
                    return;

                options.RouteBasePath = configuration.GetRequiredValue<string>("MiniProfiler:RouteBasePath");

                if (redisConnection is not null)
                    options.Storage = new RedisStorage(redisConnection.GetDatabase());

                //
                // Only the allowed user can see the profiling results
                //

                options.ResultsAuthorizeAsync = options.ResultsListAuthorizeAsync = async static req =>
                {
                    IConfigurationSection profilerConfig = req
                        .HttpContext
                        .RequestServices
                        .GetRequiredService<IConfiguration>()
                        .GetRequiredSection("MiniProfiler");

                    if (!req.Path.StartsWithSegments(profilerConfig.GetRequiredValue<string>("RouteBasePath"), StringComparison.OrdinalIgnoreCase))
                        return false;

                    IIdentity? identity = req.HttpContext.User.Identity;
                    if (identity?.IsAuthenticated is not true)
                    {
                        AuthenticateResult result = await req.HttpContext.AuthenticateAsync(WarehouseAuthentication.SCHEME);
                        if (result.Succeeded)
                            identity = result.Principal.Identity;
                    }

                    return identity?.IsAuthenticated is true && identity.Name == profilerConfig.GetRequiredValue<string>("AllowedUser");
                };
            });

            return services;
        }

        public static IApplicationBuilder UseProfiling(this IApplicationBuilder builder)
        {
            if (builder.ApplicationServices.GetRequiredService<IConfiguration>().GetSection("MiniProfiler").Exists())
                builder.UseMiniProfiler();
            return builder;
        }
    }
}
