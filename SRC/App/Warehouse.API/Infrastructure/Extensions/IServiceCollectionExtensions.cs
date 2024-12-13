using Amazon.SecretsManager;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.API.Infrastructure.Extensions
{
    using Auth;
    using Db;

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

        public static IServiceCollection AddMySQL(this IServiceCollection services)
        {
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.AddMemoryCache();
            services.TryAddScoped<MySqlConnectionFactory>();
            services.TryAddScoped(static serviceProvider => serviceProvider.GetRequiredService<MySqlConnectionFactory>().CreateConnection());

            return services;
        }
    }
}
