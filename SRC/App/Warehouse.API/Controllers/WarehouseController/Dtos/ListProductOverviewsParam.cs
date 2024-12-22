using System;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;

    /// <summary>
    /// <see cref="WarehouseController.ListProductOverviews(ListProductOverviewsParam)"/> request parameter.
    /// </summary>
    public sealed class ListProductOverviewsParam
    {
        public sealed class DecimalFilter : PropertyFilter<decimal, StructComparisonType>
        {
            [Required, AllowedValues(nameof(ProductOverview.Price))]
            public override required string Property { get; init; }

            [Required, Range(0, double.MaxValue)]
            public override required decimal Value { get; init; }
        }

        public sealed class DateFilter : PropertyFilter<DateTime, StructComparisonType>
        {
            [Required, AllowedValues(nameof(ProductOverview.ReleaseDateUtc))]
            public override required string Property { get; init; }
        }

        public sealed class StringFilter : PropertyFilter<string, StringComparisonType>
        {
            [Required, AllowedValues(nameof(ProductOverview.Name), nameof(ProductOverview.Brand))]
            public override required string Property { get; init; }
        }

        public sealed class IntFilter : PropertyFilter<int, StructComparisonType>
        {
            [Required, AllowedValues]  // no properties are bound
            public override required string Property { get; init; }
        }

        public sealed class SortProperties : SortBy
        {
            [Required, AllowedValues(nameof(ProductOverview.Name), nameof(ProductOverview.Brand), nameof(ProductOverview.Price), nameof(ProductOverview.ReleaseDateUtc))]
            public override required string Property { get; init; }
        }

        /// <summary>
        /// Filter to be used.
        /// </summary>
        [Required, ValidateObject]
        public required Filter<IntFilter, DecimalFilter, DateFilter, StringFilter> Filter { get; init; }

        /// <summary>
        /// Sorting to be applied on result.
        /// </summary>
        [ValidateObject]
        public SortBy<SortProperties>? SortBy { get; init; }

        /// <summary>
        /// Pagination config. If not provided the first 10 item is returend
        /// </summary>
        [Required, ValidateObject]
        public PaginationConfig Page { get; init; } = PaginationConfig.Default;
    }
}
