/********************************************************************************
* ListProductOverviewsParam.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Warehouse.DAL
{
    public sealed class ListProductOverviewsParam
    {
        public required Expression<Func<ProductOverview, bool>> Filter { get; init; }

        public required List<(Expression<Func<ProductOverview, object>> Property, bool Asc)> SortBy { get; init; }

        public required uint Skip { get; init; }

        public required uint Take { get; init; }
    }
}
