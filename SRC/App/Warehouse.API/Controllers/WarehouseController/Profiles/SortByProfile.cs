/********************************************************************************
* SortByProfile.cs                                                              *
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
    using static Controllers.ListProductOverviewsParam;

    using MappedSortProperties = (Expression<Func<DAL.ProductOverview, object>> Property, bool Asc);

    internal sealed class SortByProfile : Profile
    {
        #region Private
        private static readonly IReadOnlyDictionary<string, PropertyInfo> _publicProps = typeof(DAL.ProductOverview)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(static prop => prop.Name, StringComparer.OrdinalIgnoreCase);

        private static MappedSortProperties SortPropertiesConverter(SortProperties source, MappedSortProperties destination, ResolutionContext context)
        {
            if (_publicProps.TryGetValue(source.Property, out PropertyInfo? prop))
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
                    source.Kind == SortKind.Ascending
                );
            }

            throw new AutoMapperMappingException();
        }

        private static IEnumerable<MappedSortProperties> SortByConverter(SortBy<SortProperties> soruce, IEnumerable<MappedSortProperties> destination, ResolutionContext context) =>
            context.Mapper.Map<IEnumerable<MappedSortProperties>>(soruce.Properties);
        #endregion

        public SortByProfile()
        {
            CreateMap<SortProperties, MappedSortProperties>()
                .ConvertUsing(SortPropertiesConverter);
            CreateMap<SortBy<SortProperties>, IEnumerable<MappedSortProperties>>()
                .ConvertUsing(SortByConverter);
        }
    }
}
