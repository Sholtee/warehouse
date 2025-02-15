/********************************************************************************
* RedisHealthCheck.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Warehouse.Host.Services
{
    internal sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
        {
            try
            {
                await connection.GetDatabase().PingAsync();
                return HealthCheckResult.Healthy();
            }
            #pragma warning disable CA1031 // We want to catch everything
            catch (Exception ex)
            #pragma warning restore CA1031
            {
                return HealthCheckResult.Unhealthy("Could not ping the Redis instance", ex);
            }
        }
    }
}
