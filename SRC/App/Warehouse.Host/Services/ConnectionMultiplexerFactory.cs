/********************************************************************************
* ConnectionMultiplexerFactory.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Warehouse.Host.Services
{
    using Core.Extensions;

    internal sealed class ConnectionMultiplexerFactory(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        public IConnectionMultiplexer CreateConnectionMultiplexer()
        {
            ConfigurationOptions opts = ConfigurationOptions.Parse
            (
                configuration.GetRequiredValue<string>("WAREHOUSE_REDIS_CONNECTION")
            );
            opts.LoggerFactory = loggerFactory;

            return ConnectionMultiplexer.Connect(opts);
        }
    }
}
