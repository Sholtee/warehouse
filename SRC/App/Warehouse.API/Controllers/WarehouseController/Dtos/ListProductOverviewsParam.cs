/********************************************************************************
* ListProductOverviewsParam.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;

    /// <summary>
    /// <see cref="WarehouseController.ListProductOverviews(ListProductOverviewsParam)"/> request parameter.
    /// </summary>
    public sealed class ListProductOverviewsParam
    {
        #region Property selectors
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
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
        #pragma warning restore CS1591
        #endregion

        /// <summary>
        /// Filter to be used. For instance
        /// <code>(Brand == "Samsung" &amp;&amp; "Price" &lt; 1000) || (Brand == "Sony" &amp;&amp; "Price" &lt; 1500)</code>
        /// can be translated as
        /// <code>
        /// {
        ///   Block: {
        ///     String: {
        ///       Property: "Brand",
        ///       Comparison: "Equals",
        ///       Value: "Samsung"
        ///     },
        ///     And: {
        ///       Decimal: {
        ///         Property: "Price",
        ///         Comparison: "LessThan",
        ///         Value: 1000
        ///       }
        ///     }
        ///   },
        ///   Or: {
        ///     Block: {
        ///       String: {
        ///         Property: "Brand",
        ///         Comparison: "Equals",
        ///         Value: "Sony"
        ///       },
        ///       And: {
        ///         Decimal: {
        ///           Property: "Price",
        ///           Comparison: "LessThan",
        ///           Value: 1500
        ///         }
        ///       }
        ///     }
        ///   }
        /// }
        /// </code>
        /// </summary>
        [Required, ValidateObject]
        public required Filter<IntFilter, DecimalFilter, DateFilter, StringFilter> Filter { get; init; }

        /// <summary>
        /// If provided, specifies the sort properties (in order). For instance:
        /// <code>
        /// {
        ///   Properties: [
        ///     {Property: "Brand", Kind: "Ascending"},
        ///     {Property: "Name", Kind: "Ascending"}
        ///   ]
        /// }
        /// </code>
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SortBy<SortProperties>? SortBy { get; init; }

        /// <summary>
        /// Pagination config. If not provided the first 10 item is returned
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Pagination Page { get; init; } = Pagination.Default;
    }
}
