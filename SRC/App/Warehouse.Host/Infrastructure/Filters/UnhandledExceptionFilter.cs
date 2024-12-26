/********************************************************************************
* UnhandledExceptionFilter.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Infrastructure.Filters
{
    using Core.Exceptions;
    using Core.Extensions;

    /// <summary>
    /// Error details.
    /// </summary>
    #pragma warning disable CA1515 // This type is part of the public API
    public sealed class ErrorDetails
    #pragma warning restore CA1515
    {
        /// <summary>
        /// Short, human readable description of the error.
        /// </summary>
        public required string Title { get; init; }

        /// <summary>
        /// HTTP status code.
        /// </summary>
        public required int Status { get; init; }

        /// <summary>
        /// Unique identifier of the request (logs entries should contain the same id).
        /// </summary>
        public required string TraceId { get; init; }

        /// <summary>
        /// Detailed error information (should NOT contain sensitive information).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Errors { get; init; }

        /// <summary>
        /// Message to the devs (may contain sensitive information). Won't be set in production environment.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? DeveloperMessage { get; init; }
    }

    internal sealed class UnhandledExceptionFilter(IWebHostEnvironment env, ILogger<UnhandledExceptionFilter> logger) : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is RequestException requestException)
            {
                context.Result = GetResult(requestException.HttpStatus, requestException.Errors, requestException.DeveloperMessage);
            }
            else
            {
                logger.LogError(new EventId(context.Exception.HResult), "Unhandled exception occured: {exception}", context.Exception);

                context.Result = GetResult(StatusCodes.Status500InternalServerError, null, context.Exception.ToString());
            }

            context.ExceptionHandled = true;

            ObjectResult GetResult(int statusCode, object? errors, object? developerMessage)
            {
                ErrorDetails result = new()
                {
                    Title = ReasonPhrases.GetReasonPhrase(statusCode),
                    Status = statusCode,
                    TraceId = context.HttpContext.TraceIdentifier,
                    Errors = errors,
                    DeveloperMessage = env.IsLocal() || env.IsDev() ? developerMessage : null
                };

                return new ObjectResult(result)
                {
                    StatusCode = statusCode
                };
            }
        }
    }
}
