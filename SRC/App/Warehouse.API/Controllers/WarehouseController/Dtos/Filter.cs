using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    public enum StructComparisonType
    {
        Equals, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual
    }

    public enum StringComparisonType
    {
        Equals, NotEquals, Like, NotLike
    }

    public abstract class PropertyFilter<TValue, TComparison> where TComparison: struct, Enum
    {
        /// <summary>
        /// Abstract to let the descendant put <see cref="AllowedValuesAttribute"/> on it
        /// </summary>
        public required abstract string Property { get; init; }

        /// <summary>
        /// The comparison
        /// </summary>
        public required TComparison Comparison { get; init; }

        /// <summary>
        /// Value to compare to
        /// </summary>
        public required TValue Value { get; init; }
    }

    /// <summary>
    /// Entity to describe primitive filter patterns. For instance
    /// <code>(Brand == "Samsung" && "Price" < 1000) || (Brand == "Sony" && "Price" < 1500)</code>
    /// can be translated as
    /// <code>
    /// {
    ///   Block: {
    ///     String: {
    ///       Property: "Brand,
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
    ///         Property: "Brand,
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
    public class Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>: PaginationConfig, IValidatableObject
        where TIntFilter: PropertyFilter<int, StructComparisonType>
        where TDecimalFilter: PropertyFilter<decimal, StructComparisonType>
        where TDateFilter: PropertyFilter<DateTime, StructComparisonType>
        where TStringFilter: PropertyFilter<string, StringComparisonType>
    {
        public TIntFilter? Int { get; init; }

        public TDecimalFilter? Decimal { get; init; }

        public TDateFilter? Date { get; init; }

        public TStringFilter? String { get; init; }

        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? Block { get; init; }

        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? And { get; init; }

        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? Or { get; init; }

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (And is not null && Or is not null)
                yield return new ValidationResult($"'{nameof(And)}' and '{nameof(Or)}' cannot be provided simultaneously", [nameof(And), nameof(Or)]);

            int notNull = 0;
            foreach (object? filter in new object?[] { Int, Decimal, Date, String, Block })
            {
                if (filter is not null)
                    notNull++;
            }

            if (notNull is not 1)
                yield return new ValidationResult("One filter must be provided", [nameof(Int), nameof(Decimal), nameof(Date), nameof(String), nameof(Block)]);
        }
    }
}
