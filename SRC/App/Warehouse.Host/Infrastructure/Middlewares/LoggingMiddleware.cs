/********************************************************************************
* LoggingMiddleware.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;


namespace Warehouse.Host.Infrastructure.Middlewares
{
    internal sealed class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            using (logger.BeginScope(new { Client = context.User?.Identity?.Name }))
            {
                await next(context);
            }
        }
    }
}
