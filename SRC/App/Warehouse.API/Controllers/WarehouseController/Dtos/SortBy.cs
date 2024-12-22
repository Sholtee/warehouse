using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;

    public enum SortKind { Ascending, Descending }

    /// <summary>
    /// Specifies a property by which we want to sort
    /// </summary>
    public abstract class SortBy
    {
        /// <summary>
        /// Abstract to let the descendant put <see cref="AllowedValuesAttribute"/> on it
        /// </summary>
        public required abstract string Property { get; init; }

        /// <summary>
        /// Asc or Desc
        /// </summary>
        [Required]
        public required SortKind Kind { get; init; }
    }

    /// <summary>
    /// Entity describing sorting patterns. For instance
    /// <code>
    /// {Properties: [{Property: "Price", Kind: "Descending"}]}
    /// </code>
    /// </summary>
    public sealed class SortBy<TSortBy> where TSortBy: SortBy
    {
        /// <summary>
        /// Properties by which we want to sort.
        /// </summary>
        [Required, ValidateObject(validateItems: true)]
        public required List<TSortBy> Properties { get; init; }
    }
}
