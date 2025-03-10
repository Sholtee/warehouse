/********************************************************************************
* Registrations.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.DAL.Registrations
{
    using Extensions;

    /// <summary>
    /// Registers the repositories from this assembly.
    /// </summary>
    public static class Registrations
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services, nameof(services));

            services.TryAddScoped<IUserRepository, UserRepository>();
            services.TryAddScoped<IWarehouseRepository, WarehouseRepository>();

            DapperExtensions.ExtendMappers();  // does nothing if the mapper had already been registered

            return services;
        }
    }
}
