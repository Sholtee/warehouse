/********************************************************************************
* AwsHealthCheck.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Warehouse.Host.Services
{
    internal sealed class AwsHealthCheck(IAmazonSecurityTokenService sts) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest(), cancellationToken);
                return HealthCheckResult.Healthy();
            }
            #pragma warning disable CA1031 // We want to catch everything
            catch (Exception ex)
            #pragma warning restore CA1031
            {
                return HealthCheckResult.Unhealthy("Could not ping the AWS sts service", ex);
            }
        }
    }
}
