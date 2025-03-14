/********************************************************************************
* Registrations.RootUser.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Amazon.SecretsManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using DAL.Registrations;
    using Services;

    internal static partial class Registrations
    {
        public static IApplicationBuilder AddRootUser(this IApplicationBuilder self)
        {
            self.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStarted.Register(() =>
            {
                using IServiceScope scope = self.ApplicationServices.CreateScope();

                scope.ServiceProvider.GetRequiredService<RootUserRegistrar>().EnsureHasRootUser();
            });
            return self;
        }

        public static IServiceCollection AddRootUserRegistrar(this IServiceCollection services)
        {
            services.AddDbConnection();
            services.AddRepositories();

            services.TryAddAWSService<IAmazonSecretsManager>();
            services.TryAddScoped(typeof(IPasswordHasher<>), typeof(PasswordHasher<>));
            services.TryAddScoped<RootUserRegistrar>();

            return services;
        }
    }
}
