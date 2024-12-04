namespace Warehouse.API.Infrastructure.Middlewares
{
    internal class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
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
