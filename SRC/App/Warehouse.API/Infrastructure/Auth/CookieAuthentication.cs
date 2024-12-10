using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.API.Infrastructure.Auth
{
    internal static class CookieAuthentication
    {
        public const string SCHEME = "session-cookie";

        public static AuthenticationBuilder AddCookieAuthentication(this IServiceCollection services)
        {
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddScoped<IJwtService, JwtService>();
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddMemoryCache();

            return services
                .AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, CookieAuthenticationHandler>(SCHEME, null);
        }
    }
}
