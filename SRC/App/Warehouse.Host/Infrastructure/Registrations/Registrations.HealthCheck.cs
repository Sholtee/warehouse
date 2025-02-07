/********************************************************************************
* Registrations.HealthCheck.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Extensions;
    using Services;

    internal static partial class Registrations
    {
        public static IServiceCollection AddHealthCheck(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck<AwsHealthCheck>(nameof(AwsHealthCheck))
                .AddCheck<DbConnectionHealthCheck>(nameof(DbConnectionHealthCheck));
    
            return services;
        }

        public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder builder) => builder.UseHealthChecks("/healthcheck", new HealthCheckOptions
        {
            ResponseWriter = static (context, report) =>
            {
                HttpResponse resp = context.Response;

                if (report.Status is HealthStatus.Healthy)
                {
                    resp.StatusCode = StatusCodes.Status200OK;
                    return resp.WriteAsync(report.Status.ToString());
                }

                resp.StatusCode = StatusCodes.Status500InternalServerError;

                IHostEnvironment environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
                if (environment.IsLocal() || environment.IsDev())
                    //
                    // Write verbose response when running on dev environment
                    //

                    return resp.WriteAsJsonAsync(new
                    {
                        report.Status,
                        Details = report.Entries.Select(static entry => new
                        {
                            Name = entry.Key,
                            Exception = entry.Value.Exception?.Message,
                            entry.Value.Data,
                            entry.Value.Description   
                        })
                    });

                return resp.WriteAsync(report.Status.ToString());
            }
        });
    }
}
