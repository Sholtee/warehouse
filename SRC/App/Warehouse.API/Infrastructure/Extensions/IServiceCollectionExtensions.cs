using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.API.Infrastructure.Extensions
{
    using Auth;

    internal static class IServiceCollectionExtensions
    {
        public static AuthenticationBuilder AddSessionCookieAuthentication(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddScoped<IJwtService, JwtService>();
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddMemoryCache();

            return services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, SessionCookieAuthenticationHandler>("session-cookie", null);
        }
    }
}
