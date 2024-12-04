using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Warehouse.API.Infrastructure.Filters
{
    using Controllers.Exceptions;

    internal sealed class UnhandledExceptionFilter(IWebHostEnvironment env, ILogger<UnhandledExceptionFilter> logger) : IExceptionFilter
    {
        private sealed record ErrorDescriptor(string Title, int Status, string TraceId, object? Errors)
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public object? DeveloperMessage { get; init; }
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is RequestException requestException)
            {
                context.Result = GetResult(requestException.HttpStatus, requestException.DeveloperMessage, requestException.Errors);
            }
            else
            {
                logger.LogError(new EventId(context.Exception.HResult), context.Exception.ToString());

                context.Result = GetResult(StatusCodes.Status500InternalServerError, "Internal error occured", context.Exception);
            }

            context.ExceptionHandled = true;

            ObjectResult GetResult(int statusCode, object? developerMessage, object? errors)
            {
                ErrorDescriptor result = new
                (
                    Title: ReasonPhrases.GetReasonPhrase(statusCode),
                    Status: statusCode,
                    TraceId: context.HttpContext.TraceIdentifier,
                    Errors: errors
                ) { DeveloperMessage = env.IsDevelopment() ? developerMessage : null };

                return new ObjectResult(result)
                {
                    StatusCode = statusCode
                };
            }
        }
    }
}
