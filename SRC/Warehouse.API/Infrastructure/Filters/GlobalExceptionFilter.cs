using Microsoft.AspNetCore.Mvc.Filters;

namespace Warehouse.API.Infrastructure.Filters
{
    public class GlobalExceptionFilter(IWebHostEnvironment env, ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (env.IsDevelopment())
            {

            }
        }
    }
}
