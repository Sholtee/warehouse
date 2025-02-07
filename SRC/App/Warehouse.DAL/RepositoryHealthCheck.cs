/********************************************************************************
* RepositoryHealthCheck.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using ServiceStack.OrmLite;


namespace Warehouse.DAL.Registrations
{
    internal sealed class RepositoryHealthCheck(IDbConnection connection) : IRepositoryHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                int result = await connection.SqlScalarAsync<int>
                (
                    connection
                        .From<object>(static expr => expr.FromExpression = " ")
                        .Select(static _ => 1),
                    cancellationToken
                );
                if (result is not 1)
                    throw new InvalidOperationException("The query yielded unexpected value");

                return HealthCheckResult.Healthy();
            }
            #pragma warning disable CA1031 // We want to catch everything
            catch (Exception ex)
            #pragma warning restore CA1031
            {
                return HealthCheckResult.Unhealthy("The database is unhealthy", ex);
            }
        }
    }
}
