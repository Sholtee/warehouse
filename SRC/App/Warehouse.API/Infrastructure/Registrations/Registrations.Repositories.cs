using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.API.Infrastructure.Registrations
{
    using DAL;

    internal static partial class Registrations
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }
    }
}
