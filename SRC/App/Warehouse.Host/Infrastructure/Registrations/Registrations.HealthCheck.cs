/********************************************************************************
* Registrations.HealthCheck.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Linq;

using Amazon.SecurityToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Extensions;
    using Dtos;
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddHealthCheck(this IServiceCollection services)
        {
            services
                .TryAddAWSService<IAmazonSecurityTokenService>() // for AwsHealthCheck
                .AddDbConnection()
                .AddRedis();

            services.AddHealthChecks()
                .AddCheck<AwsHealthCheck>(nameof(AwsHealthCheck))
                .AddCheck<DbConnectionHealthCheck>(nameof(DbConnectionHealthCheck))
                .AddCheck<RedisHealthCheck>(nameof(RedisHealthCheck));
    
            return services;
        }

        public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder builder) => builder.UseHealthChecks("/healthcheck", new HealthCheckOptions
        {
            ResponseWriter = static (context, report) =>
            {
                HttpResponse resp = context.Response;

                resp.Headers.ContentType = "application/json";

                if (report.Status is HealthStatus.Healthy)
                {
                    resp.StatusCode = StatusCodes.Status200OK;
                }
                else
                {
                    resp.StatusCode = StatusCodes.Status500InternalServerError;

                    IHostEnvironment environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
                    if (environment.IsLocal() || environment.IsDev())
                        //
                        // Write verbose response when running on dev environment
                        //

                        return resp.WriteAsJsonAsync
                        (
                            new HealthCheckResult
                            {
                                Status = report.Status.ToString(),
                                Details = report.Entries.Select(static entry => new
                                {
                                    Name = entry.Key,
                                    Exception = entry.Value.Exception?.Message,
                                    entry.Value.Data,
                                    entry.Value.Description
                                }).ToList()
                            }
                        );
                }

                return resp.WriteAsJsonAsync
                (
                    new HealthCheckResult
                    {
                        Status = report.Status.ToString()
                    }
                );
            }
        });
    }
}
