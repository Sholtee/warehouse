using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    using static Controllers.ListProductOverviewsParam;

    internal sealed class SortByProfile : Profile
    {
        private static readonly IReadOnlyDictionary<string, PropertyInfo> _publicProps = typeof(DAL.ProductOverview)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static prop => prop.Name, StringComparer.OrdinalIgnoreCase);

        public SortByProfile()
        {
            CreateMap<SortProperties, (Expression<Func<DAL.ProductOverview, object>> Property, bool Asc)>()
                .ConvertUsing((src, dst, ctx) =>
                {
                    if (_publicProps.TryGetValue(src.Property, out PropertyInfo? prop))
                    {
                        ParameterExpression productOverview = Expression.Parameter(typeof(DAL.ProductOverview), nameof(productOverview));

                        return
                        (
                            Expression.Lambda<Func<DAL.ProductOverview, object>>
                            (
                                Expression.Convert
                                (
                                    Expression.Property(productOverview, prop),
                                    typeof(object)
                                ),
                                productOverview
                            ),
                            src.Kind == SortKind.Ascending
                        );
                    }

                    throw new AutoMapperMappingException();
                });

            CreateMap<SortBy<SortProperties>, List<(Expression<Func<DAL.ProductOverview, object>> Property, bool Asc)>>()
                .ConvertUsing
                (
                    (src, dst, ctx) => src
                        .Properties
                        .Select(prop => ctx.Mapper.Map<(Expression<Func<DAL.ProductOverview, object>> Property, bool Asc)>(prop))
                        .ToList()
                );
        }
    }
}
