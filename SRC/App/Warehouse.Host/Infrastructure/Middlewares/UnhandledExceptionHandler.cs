/********************************************************************************
* UnhandledExceptionHandler.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Warehouse.Host.Infrastructure.Middlewares
{
    using Core.Exceptions;
    using Core.Extensions;
    using Dtos;

    internal sealed class UnhandledExceptionHandler(IWebHostEnvironment env, ILogger<UnhandledExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is RequestException requestException)
            {
                logger.LogInformation(new EventId(exception.HResult), "Request exception occurred: [{status}] {details}", requestException.HttpStatus, requestException.Errors);

                requestException.PrepareResponse(httpContext.Response);

                await WriteDetailsAsync(requestException.Errors, requestException.DeveloperMessage);
            }
            else
            {
                logger.LogError(new EventId(exception.HResult), exception, "Unhandled exception occurred");

                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await WriteDetailsAsync(null, exception.ToString());
            }

            return true;

            Task WriteDetailsAsync(object? errors, object? developerMessage) => httpContext.Response.WriteAsJsonAsync
            (
                new ErrorDetails()
                {
                    Title = ReasonPhrases.GetReasonPhrase(httpContext.Response.StatusCode),
                    Status = httpContext.Response.StatusCode,
                    TraceId = httpContext.TraceIdentifier,
                    Errors = errors,
                    DeveloperMessage = env.IsLocal() || env.IsDev() ? developerMessage : null
                },
                cancellationToken
            );
        }
    }
}
