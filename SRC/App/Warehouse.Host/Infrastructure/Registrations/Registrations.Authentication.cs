/********************************************************************************
* Registrations.Authentication.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Auth;
    using Core.Abstractions;
    using Core.Auth;
    using Core.Extensions;
    using Services;

    internal static partial class Registrations
    {
        private static void AddAuthenticationBase(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddSingleton(TimeProvider.System);
            services.TryAddScoped<ISessionManager, HttpSessionManager>();
            services.AddHttpContextAccessor();
            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, AuthenticationHandler>(WarehouseAuthentication.SCHEME, null);
        }

        public static IServiceCollection AddStatefulAuthentication(this IServiceCollection services)
        {
            services.AddAuthenticationBase();
            services.AddStackExchangeRedisCache(static _ => { });
            services.AddOptions<RedisCacheOptions>().Configure<IConfiguration>
            (
                static (opts, config) => opts.Configuration = config.GetRequiredValue<string>("WAREHOUSE_REDIS_ENDPOINT")
            );         
            services.TryAddScoped<ITokenManager, CachedIdentityManager>();

            return services;
        }

        /// <summary>
        /// Not advised to use when SlidingExpiration is enabled as it may let the client create an infinite token
        /// </summary>
        public static IServiceCollection AddStatelessAuthentication(this IServiceCollection services)
        {
            services.AddAuthenticationBase();
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.TryAddScoped<ITokenManager, JwtManager>();
            services.AddMemoryCache();

            return services;
        }
    }
}
