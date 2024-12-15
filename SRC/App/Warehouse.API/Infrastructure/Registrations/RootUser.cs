using Amazon.SecretsManager;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.API.Infrastructure.Registrations
{   
    using Services;

    internal static class RootUser
    {
        public static IApplicationBuilder AddRootUser(this IApplicationBuilder self)
        {
            IHostApplicationLifetime lifetime = self.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                using IServiceScope scope = self.ApplicationServices.CreateScope();

                scope.ServiceProvider.GetRequiredService<RootUserRegistrar>().EnsureHasRootUser();
            });
            return self;
        }

        public static IServiceCollection AddRootUserRegistrar(this IServiceCollection services)
        {
            services.TryAddAWSService<IAmazonSecretsManager>();
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddScoped<RootUserRegistrar>();
            services.AddRepositories();

            return services;
        }
    }
}