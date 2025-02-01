/********************************************************************************
* RequestException.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

namespace Warehouse.Core.Exceptions
{
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
    }
}
