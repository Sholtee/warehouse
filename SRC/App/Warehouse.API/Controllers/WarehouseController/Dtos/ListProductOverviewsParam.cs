using System;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// <see cref="WarehouseController.ListProductOverviews(ListProductOverviewsParam)"/> request parameter.
    /// </summary>
    public sealed class ListProductOverviewsParam
    {
        public sealed class DecimalFilter : PropertyFilter<decimal, StructComparisonType>
        {
            [AllowedValues(nameof(ProductOverview.Price))]
            public override required string Property { get; init; }

            [Range(0, double.MaxValue)]
            public override required decimal Value { get; init; }
        }

        public sealed class DateFilter : PropertyFilter<DateTime, StructComparisonType>
        {
            [AllowedValues(nameof(ProductOverview.ReleaseDate))]
            public override required string Property { get; init; }
        }

        public sealed class NameFilter : PropertyFilter<string, StringComparisonType>
        {
            [AllowedValues(nameof(ProductOverview.Name), nameof(ProductOverview.Brand))]
            public override required string Property { get; init; }
        }

        public sealed class IntFilter : PropertyFilter<int, StructComparisonType>
        {
            [AllowedValues]  // no properties are bound
            public override required string Property { get; init; }
        }

        public sealed class SortProperties : SortBy
        {
            [AllowedValues(nameof(ProductOverview.Name), nameof(ProductOverview.Brand), nameof(ProductOverview.Price), nameof(ProductOverview.ReleaseDate))]
            public override required string Property { get; init; }
        }

        /// <summary>
        /// Filter to be used.
        /// </summary>
        public required Filter<IntFilter, DecimalFilter, DateFilter, NameFilter> Filter { get; init; }

        /// <summary>
        /// Sorting to be applied on result.
        /// </summary>
        public SortProperties? SortBy { get; init; }

        /// <summary>
        /// Pagination config. If not provided the first 10 item is returend
        /// </summary>
        public PaginationConfig Page { get; init; } = PaginationConfig.Default;
    }
}
