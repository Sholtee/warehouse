/********************************************************************************
* ErrorDetails.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Text.Json.Serialization;

namespace Warehouse.Host.Dtos
{
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
}
