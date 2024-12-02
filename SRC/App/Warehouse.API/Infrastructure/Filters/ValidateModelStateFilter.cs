using Microsoft.AspNetCore.Mvc.Filters;

namespace Warehouse.API.Infrastructure.Filters
{
    using Controllers.Exceptions;

    public sealed class ValidateModelStateFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                Dictionary<string, List<string>> validationErrors = context
                    .ModelState
                    .Where(static modelState => modelState.Value is not null)
                    .ToDictionary
                    (
                        static modelState => modelState.Key,
                        static modelState => modelState.Value!
                            .Errors
                            .Select(static err => err.ErrorMessage)
                            .ToList()
                    );

                throw new BadRequestException
                {
                    Errors = validationErrors
                };
            }
        }
    }
}
