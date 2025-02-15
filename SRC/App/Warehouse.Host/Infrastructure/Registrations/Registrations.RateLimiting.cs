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
using RedisRateLimiting;
using StackExchange.Redis;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Exceptions;

    internal static partial class Registrations
    {
        public static IServiceCollection AddRateLimiter(this IServiceCollection services) => services.AddRedis().AddRateLimiter(static opts =>
        {
            opts
                .AddPolicy("fixed", static httpContext =>
                {
                    //
                    // These are global singletons so it's safe to capture them in the factory function
                    //

                    IConfiguration configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                    IConnectionMultiplexer connectionMultiplexer = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();

                    return RedisRateLimitPartition.GetFixedWindowRateLimiter("fixed", _ =>
                    {
                        RedisFixedWindowRateLimiterOptions opts = new();
                        configuration.GetRequiredSection("RateLimiting:Fixed").Bind(opts);

                        //
                        // Despite the factory pattern the library never frees the created multiplexer instances
                        // https://github.com/cristipufu/aspnetcore-redis-rate-limiting/issues/200
                        //

                        opts.ConnectionMultiplexerFactory = () => connectionMultiplexer;
                        return opts;
                    });
                })
                .AddPolicy("userBound", static httpContext =>
                {
                    string? userId = httpContext.User.Identity?.Name;

                    IConfiguration configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                    IConnectionMultiplexer connectionMultiplexer = httpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();

                    return string.IsNullOrEmpty(userId)
                        ? GetLimiter("anon", "Anon")
                        : GetLimiter(userId, "UserBound");

                    RateLimitPartition<string> GetLimiter(string partitionKey, string configKey) => RedisRateLimitPartition.GetTokenBucketRateLimiter(partitionKey, _ =>
                    {
                        RedisTokenBucketRateLimiterOptions opts = new();
                        configuration.GetRequiredSection($"RateLimiting:{configKey}").Bind(opts);
                        opts.ConnectionMultiplexerFactory = () => connectionMultiplexer;
                        return opts;
                    });
                });

            opts.OnRejected = static (context, _) =>
            {
                context
                    .HttpContext
                    .RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiter")
                    .LogWarning("Too many requests on endpoint: {endpoint}", context.HttpContext.GetEndpoint()?.DisplayName);
                throw new TooManyRequestsException();
            };
        });
    }
}
