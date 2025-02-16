/********************************************************************************
* RequestException.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.AspNetCore.Http;

namespace Warehouse.Core.Exceptions
{
    /// <summary>
    /// Base class of request exceptions. Transformed to HTTP response using the given <see cref="HttpStatus"/>
    /// </summary>
    public abstract class RequestException() : Exception("Request exception occurred")
    {
        /// <summary>
        /// Additional information regarding the error that might contain sensitive data.
        /// </summary>
        public object? DeveloperMessage { get; init; }

        /// <summary>
        /// Additional information regarding the error NOT containing sensitive data (such as validation errors)
        /// </summary>
        public object? Errors { get; init; }

        /// <summary>
        /// The HTTP status code
        /// </summary>
        public abstract int HttpStatus { get; }

        /// <summary>
        /// Prepares the HTTP response. Content should not be written here.
        /// </summary>
        public virtual void PrepareResponse(HttpResponse response)
        {
            ArgumentNullException.ThrowIfNull(response, nameof(response));

            response.StatusCode = HttpStatus;
        }
    }
}
