/********************************************************************************
* Pagination.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// Pagination config
    /// </summary>
    public sealed class Pagination
    {
        /// <summary>
        /// The default value. Returns the first 10 element.
        /// </summary>
        public static readonly Pagination Default = new();

        /// <summary>
        /// Pages to skip. Set to the first page if not provided.
        /// </summary>
        [Range(0, uint.MaxValue)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint Skip { get; init; } = 0;

        /// <summary>
        /// Page size. Set to 10 if not provided
        /// </summary>
        [Range(1, 50)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint Size { get; init; } = 10;
    }
}
