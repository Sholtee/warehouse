using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.DAL.Registrations
{
    /// <summary>
    /// Registers the repositories from this assembly.
    /// </summary>
    public static class Registrations
    {
        public static IServiceCollection AddWarehouseRepositories(this IServiceCollection services)
        {
            services.TryAddScoped<IUserRepository, UserRepository>();
            services.TryAddScoped<IWarehouseRepository, WarehouseRepository>();

            return services;
        }
    }
}
