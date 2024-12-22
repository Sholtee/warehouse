/********************************************************************************
* FilterProfile.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    using ComparisonExpression     = Func<Expression, Expression, Expression>;
    using UnionFilter              = Filter<Controllers.ListProductOverviewsParam.IntFilter, Controllers.ListProductOverviewsParam.DecimalFilter, Controllers.ListProductOverviewsParam.DateFilter, Controllers.ListProductOverviewsParam.StringFilter>;
    using OverviewFilterExpression = Expression<Func<DAL.ProductOverview, bool>>;

    internal sealed class FilterProfile : Profile
    {
        #region Private
        private static readonly ParameterExpression _productOverview = Expression.Parameter(typeof(DAL.ProductOverview), "productOverview");

        private static readonly IReadOnlyDictionary<string, PropertyInfo> _publicProps = typeof(DAL.ProductOverview)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static prop => prop.Name, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<StructComparisonType, ComparisonExpression> _structComparisons = new Dictionary<StructComparisonType, ComparisonExpression>
        {
            { StructComparisonType.Equals, Expression.Equal },
            { StructComparisonType.NotEquals, Expression.NotEqual },
            { StructComparisonType.LessThanOrEqual, Expression.LessThanOrEqual },
            { StructComparisonType.LessThan, Expression.LessThan },
            { StructComparisonType.GreaterThanOrEqual, Expression.GreaterThanOrEqual },
            { StructComparisonType.GreaterThan, Expression.GreaterThan }
        };

        private static readonly MethodInfo _contains = ((MethodCallExpression) ((Expression<Func<string, string, bool>>) ((left, right) => left.Contains(right))).Body).Method;

        private static readonly IReadOnlyDictionary<StringComparisonType, ComparisonExpression> _stringComparisons = new Dictionary<StringComparisonType, ComparisonExpression>
        {
            { StringComparisonType.Equals, Expression.Equal },
            { StringComparisonType.NotEquals, Expression.NotEqual },
            { StringComparisonType.Like, static (left, right) => Expression.Call(left, _contains, right) },
            { StringComparisonType.NotLike, static (left, right) => Expression.Not(Expression.Call(left, _contains, right)) }
        };

        private static Expression FilterConverter<TSrc, TComparison>(PropertyFilter<TSrc, TComparison> source, IReadOnlyDictionary<TComparison, ComparisonExpression> comparisons) where TComparison : struct, Enum
        {
            if (_publicProps.TryGetValue(source.Property, out PropertyInfo? prop) && comparisons.TryGetValue(source.Comparison, out ComparisonExpression? exprFactory))
            {
                return exprFactory(Expression.Property(_productOverview, prop), Expression.Constant(source.Value));
            }

            throw new AutoMapperMappingException();
        }

        private static Expression FilterConverter<TSrc>(PropertyFilter<TSrc, StructComparisonType> source, Expression destination, ResolutionContext context) =>
            FilterConverter(source, _structComparisons);

        private static Expression FilterConverter(PropertyFilter<string, StringComparisonType> source, Expression destination, ResolutionContext context) =>
            FilterConverter(source, _stringComparisons);

        private static Expression FilterConverter(UnionFilter source, Expression destination, ResolutionContext context)
        {
            Expression left = context.Mapper.Map<Expression>
            (
                source.Block /*Block has the priority*/ ?? source.Int ?? source.Decimal ?? source.Date ?? (object) source.String!
            );

            if (source.Or is not null)
                return Expression.Or(left, context.Mapper.Map<Expression>(source.Or));

            if (source.And is not null)
                return Expression.And(left, context.Mapper.Map<Expression>(source.And));

            return left;
        }

        private static OverviewFilterExpression FilterConverter(UnionFilter source, OverviewFilterExpression destination, ResolutionContext context) => Expression.Lambda<Func<DAL.ProductOverview, bool>>
        (
            context.Mapper.Map<Expression>(source),
            _productOverview
        );
        #endregion

        public FilterProfile()
        {
            CreateMap<Controllers.ListProductOverviewsParam.DecimalFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<Controllers.ListProductOverviewsParam.IntFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<Controllers.ListProductOverviewsParam.DateFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<Controllers.ListProductOverviewsParam.StringFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<UnionFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<UnionFilter, OverviewFilterExpression>()
                .ConvertUsing(FilterConverter);
        }
    }
}
