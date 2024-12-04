using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Dtos
{
    /// <summary>
    /// Product filter, used in queries.
    /// </summary>
    public class ProductFilter
    {
        /// <summary>
        /// If set, filters to full or partial product names
        /// </summary>
        public string? NameLike { get; init; }

        /// <summary>
        /// If set, filters to the condition.
        /// </summary>
        public ProductCondition? Condition { get; init; }

        /// <summary>
        /// If provided, sets the lower price limit.
        /// </summary>
        [Range(0, int.MaxValue)]
        public decimal? PriceOver { get; init; }

        /// <summary>
        /// If procided, sets the upper price limit.
        /// </summary>
        [Range(0, int.MaxValue)]
        public decimal? PriceUnder { get; init; }

        /// <summary>
        /// Pages to skip.
        /// </summary>
        [Range(0, uint.MaxValue)]
        public uint? SkipPages { get; init; } = 0;

        /// <summary>
        /// Page size.
        /// </summary>
        [Range(1, 50)]
        public uint? PageSize { get; init; } = 10;
    }
}
