/********************************************************************************
* Filter.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;

    /// <summary>
    /// Comparison types for structs 
    /// </summary>
    #pragma warning disable CS1591  // Missing XML comment for publicly visible type or member
    public enum StructComparisonType { Equals, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual }

    /// <summary>
    /// Comparison types for strings
    /// </summary>
    public enum StringComparisonType { Equals, NotEquals, Like, NotLike }
    #pragma warning restore CS1591

    /// <summary>
    /// Base class of property filters.
    /// </summary>
    public abstract class PropertyFilter<TValue, TComparison> where TComparison: struct, Enum
    {
        /// <summary>
        /// Abstract to let the descendant put <see cref="AllowedValuesAttribute"/> on it
        /// </summary>
        public required abstract string Property { get; init; }

        /// <summary>
        /// The comparison
        /// </summary>
        [Required]
        public required TComparison Comparison { get; init; }

        /// <summary>
        /// Value to compare to
        /// </summary>
        [Required]
        public virtual required TValue Value { get; init; }
    }

    /// <summary>
    /// Entity to describe primitive filter patterns. For instance
    /// <code>(Brand == "Samsung"  &amp;&amp; "Price" &lt; 1000) || (Brand == "Sony" &amp;&amp; "Price" &lt; 1500)</code>
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
    public sealed class Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>: IValidatableObject
        where TIntFilter: PropertyFilter<int, StructComparisonType>
        where TDecimalFilter: PropertyFilter<decimal, StructComparisonType>
        where TDateFilter: PropertyFilter<DateTime, StructComparisonType>
        where TStringFilter: PropertyFilter<string, StringComparisonType>
    {
        /// <summary>
        /// Int filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TIntFilter? Int { get; init; }

        /// <summary>
        /// Decimal filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TDecimalFilter? Decimal { get; init; }

        /// <summary>
        /// Date filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TDateFilter? Date { get; init; }

        /// <summary>
        /// String filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public TStringFilter? String { get; init; }

        /// <summary>
        /// Condition block
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? Block { get; init; }

        /// <summary>
        /// "and" filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? And { get; init; }

        /// <summary>
        /// "or" filter
        /// </summary>
        [ValidateObject]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Filter<TIntFilter, TDecimalFilter, TDateFilter, TStringFilter>? Or { get; init; }

        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
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
