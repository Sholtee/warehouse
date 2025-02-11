/********************************************************************************
* Registrations.Authentication.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Auth;
    using Core.Abstractions;
    using Core.Auth;
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddSessionCookieAuthentication(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddScoped<IJwtService, JwtService>(); 
            services.TryAddScoped<ISessionManager, HttpSessionManager>();
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>(WarehouseAuthentication.SCHEME, null);

            return services;
        }
    }
}
