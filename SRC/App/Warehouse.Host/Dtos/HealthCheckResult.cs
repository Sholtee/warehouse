/********************************************************************************
* HealthCheckResult.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Text.Json.Serialization;

namespace Warehouse.Host.Dtos
{
    /// <summary>
    /// Health check result
    /// </summary>
    #pragma warning disable CA1515 // This type is part of the public API
    public sealed class HealthCheckResult
    #pragma warning restore CA1515
    {
        /// <summary>
        /// Short, human readable status.
        /// </summary>
        public required string Status { get; init; }

        /// <summary>
        /// Detailed error information (should not be present in production environments).
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Details { get; init; }
    }
}
