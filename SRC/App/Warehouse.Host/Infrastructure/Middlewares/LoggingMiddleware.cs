/********************************************************************************
* LoggingMiddleware.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace Warehouse.Host.Infrastructure.Middlewares
{
    internal sealed class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            ClaimsPrincipal user = context.User;

            Dictionary<string, object> scope = new()
            {
                {
                    "@Client",
                    new
                    {
                        Id = user
                            .Identity
                            ?.Name ?? "Anonymous",
                        Roles =  user
                            .Claims
                            .Where(static claim => claim.Type == ClaimTypes.Role)
                            .Select(static claim => claim.Value)
                    }
                }
            };

            using(logger.BeginScope(scope))
            {
                await next(context);
            }
        }
    }
}
