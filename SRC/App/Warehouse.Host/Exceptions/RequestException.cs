using System;

namespace Warehouse.API.Exceptions
{
    internal abstract class RequestException : Exception
    {
        /// <summary>
        /// Additional information regarding the error that might contain sensitive data.
        /// </summary>
        public object? DeveloperMessage { get; init; }

        /// <summary>
        /// Additional information regarding the error NOT contianing sensitive data (such as validation errors)
        /// </summary>
        public object? Errors { get; init; }

        /// <summary>
        /// The HTTP status code
        /// </summary>
        public abstract int HttpStatus { get; }
    }
}
