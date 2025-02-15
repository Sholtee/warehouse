/********************************************************************************
* Registrations.Redis.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddRedis(this IServiceCollection services)
        {
            services.TryAddSingleton<ConnectionMultiplexerFactory>();
            services.TryAddSingleton(static serviceProvider => serviceProvider.GetRequiredService<ConnectionMultiplexerFactory>().CreateConnectionMultiplexer());
            services.AddStackExchangeRedisCache(static _ => { }).AddOptions<RedisCacheOptions, ConnectionMultiplexerFactory>
            (
                static (opts, multiplexerFactory) => opts.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexerFactory.CreateConnectionMultiplexer())
            );
            return services;
        }
    }
}
