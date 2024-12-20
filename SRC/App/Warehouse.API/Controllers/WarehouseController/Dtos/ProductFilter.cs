using System;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    using static ProductFilter;

    /// <summary>
    /// Product filter, used in queries.
    /// </summary>
    public sealed class ProductFilter: FilterBase<IntFilter, DecimalFilter, DateFilter, NameFilter>
    {
        public sealed class DecimalFilter : PropertyFilter<decimal, StructComparisonType>
        {
            [AllowedValues(nameof(ProductOverview.Price))]
            public override required string Property { get; init; }
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
    }
}
