/********************************************************************************
* Registrations.RateLimiting.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Infrastructure.Registrations
{
    using Core.Exceptions;

    internal static partial class Registrations
    {
        public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration configuration) => services.AddRateLimiter(opts =>
        {
            opts.AddFixedWindowLimiter("fixed", opts => configuration.GetRequiredSection("RateLimiting:Fixed").Bind(opts));

            opts.OnRejected = static (context, _) =>
            {
                context
                    .HttpContext
                    .RequestServices
                    .GetService<ILoggerFactory>()!
                    .CreateLogger("RateLimiter")
                    .LogWarning("Too many requests on endpoint: {endpoint}", context.HttpContext.GetEndpoint()?.DisplayName);
                throw new TooManyRequests();
            };
        });
    }
}
