/********************************************************************************
* Registrations.RateLimiting.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Exceptions;

    internal static partial class Registrations
    {
        public static IServiceCollection AddRateLimiter(this IServiceCollection services) => services.AddRateLimiter(static opts =>
        {
            opts
                .AddPolicy("fixed", static httpContext => RateLimitPartition.GetFixedWindowLimiter("fixed", _ =>
                {
                    FixedWindowRateLimiterOptions opts = new();
                    httpContext.RequestServices.GetRequiredService<IConfiguration>().GetRequiredSection("RateLimiting:Fixed").Bind(opts);
                    return opts;
                }))
                .AddPolicy("userBound", static httpContext =>
                {
                    string? userId = httpContext.User.Identity?.Name;

                    return string.IsNullOrEmpty(userId)
                        ? GetLimiter("anon", "Anon")
                        : GetLimiter(userId, "UserBound");

                    RateLimitPartition<string> GetLimiter(string partitionKey, string config) => RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
                    {
                        TokenBucketRateLimiterOptions opts = new()
                        {
                            AutoReplenishment = true
                        };
                        httpContext.RequestServices.GetRequiredService<IConfiguration>().GetRequiredSection($"RateLimiting:{config}").Bind(opts);
                        return opts;
                    });
                });

            opts.OnRejected = static (context, _) =>
            {
                context
                    .HttpContext
                    .RequestServices
                    .GetService<ILoggerFactory>()!
                    .CreateLogger("RateLimiter")
                    .LogWarning("Too many requests on endpoint: {endpoint}", context.HttpContext.GetEndpoint()?.DisplayName);
                throw new TooManyRequestsException();
            };
        });
    }
}
