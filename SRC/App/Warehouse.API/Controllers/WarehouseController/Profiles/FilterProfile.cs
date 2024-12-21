using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    using static Controllers.ListProductOverviewsParam;

    internal sealed class FilterProfile : Profile
    {
        #region Private
        private static readonly ParameterExpression _productOverview = Expression.Parameter(typeof(DAL.ProductOverview), "productOverview");

        private static readonly IReadOnlyDictionary<string, PropertyInfo> _publicProps = typeof(DAL.ProductOverview)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static prop => prop.Name, StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<StructComparisonType, Func<Expression, Expression, Expression>> _structComparisons = new Dictionary<StructComparisonType, Func<Expression, Expression, Expression>>
        {
            { StructComparisonType.Equals, Expression.Equal },
            { StructComparisonType.NotEquals, Expression.NotEqual },
            { StructComparisonType.LessThanOrEqual, Expression.LessThanOrEqual },
            { StructComparisonType.LessThan, Expression.LessThan },
            { StructComparisonType.GreaterThanOrEqual, Expression.GreaterThanOrEqual },
            { StructComparisonType.GreaterThan, Expression.GreaterThan }
        };

        private static readonly MethodInfo _contains = ((MethodCallExpression) ((Expression<Func<string, string, bool>>) ((left, right) => left.Contains(right))).Body).Method;

        private static readonly IReadOnlyDictionary<StringComparisonType, Func<Expression, Expression, Expression>> _stringComparisons = new Dictionary<StringComparisonType, Func<Expression, Expression, Expression>>
        {
            { StringComparisonType.Equals, Expression.Equal },
            { StringComparisonType.NotEquals, Expression.NotEqual },
            { StringComparisonType.Like, static (left, right) => Expression.Call(left, _contains, right) },
            { StringComparisonType.NotLike, static (left, right) => Expression.Not(Expression.Call(left, _contains, right)) }
        };

        private static Expression FilterConverter<TSrc, TComparison>(PropertyFilter<TSrc, TComparison> source, IReadOnlyDictionary<TComparison, Func<Expression, Expression, Expression>> comparisons, ResolutionContext context) where TComparison : struct, Enum
        {
            if (_publicProps.TryGetValue(source.Property, out PropertyInfo? prop) && comparisons.TryGetValue(source.Comparison, out Func<Expression, Expression, Expression>? exprFactory))
            {
                return exprFactory(Expression.Property(_productOverview, prop), Expression.Constant(source.Value));
            }

            throw new AutoMapperMappingException();
        }

        private static Expression FilterConverter<TSrc>(PropertyFilter<TSrc, StructComparisonType> source, Expression destination, ResolutionContext context) =>
            FilterConverter(source, _structComparisons, context);

        private static Expression FilterConverter(PropertyFilter<string, StringComparisonType> source, Expression destination, ResolutionContext context) =>
            FilterConverter(source, _stringComparisons, context);

        private static Expression FilterConverter(Filter<IntFilter, DecimalFilter, DateFilter, StringFilter> source, Expression destination, ResolutionContext context)
        {
            Expression left = source.Block is not null
                ? FilterConverter(source.Block, destination, context)  // Block has the priority
                : MapToExpression(source.Int ?? source.Decimal ?? source.Date ?? (object) source.String!);

            if (source.Or is not null)
                return Expression.Or(left, MapToExpression(source.Or));

            if (source.And is not null)
                return Expression.And(left, MapToExpression(source.And));

            return left;

            Expression MapToExpression(object val) => context.Mapper.Map<Expression>(val);
        }

        private static Expression<Func<DAL.ProductOverview, bool>> FilterConverter(Filter<IntFilter, DecimalFilter, DateFilter, StringFilter> source, Expression<Func<DAL.ProductOverview, bool>> destination, ResolutionContext context) => Expression.Lambda<Func<DAL.ProductOverview, bool>>
        (
            context.Mapper.Map<Expression>(source),
            _productOverview
        );
        #endregion

        public FilterProfile()
        {
            CreateMap<DecimalFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<IntFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<DateFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<StringFilter, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<Filter<IntFilter, DecimalFilter, DateFilter, StringFilter>, Expression>()
                .ConvertUsing(FilterConverter);
            CreateMap<Filter<IntFilter, DecimalFilter, DateFilter, StringFilter>, Expression<Func<DAL.ProductOverview, bool>>>()
                .ConvertUsing(FilterConverter);
        }
    }
}
