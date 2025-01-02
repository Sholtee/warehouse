/********************************************************************************
* WarehouseRepository.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Warehouse.DAL
{
    /// <summary>
    /// TODO: implement
    /// </summary>
    internal sealed class WarehouseRepository(IDbConnection connection, IOrmLiteDialectProvider dialectProvider) : IWarehouseRepository
    {
        public async Task<bool> IsHealthy() => await connection.SqlScalarAsync<int>("SELECT 1") is 1;

        public Task<ProductDetails?> GetProductDetailsById(Guid productId) => Task.FromResult
        (
            productId != Guid.Empty ? null : new ProductDetails
            {
                Id = productId,
                Brand = "Samsung",
                Name = "Galaxy Tab A9+",
                Types = ["tablet"],
                Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                Quantity = 10,
                Price = 10000,
                ReleaseDateUtc = new DateTime(2023, 10, 17),
                MainImage = "main.image",
                Images = []
            }
        );

        public Task<List<ProductOverview>> ListProductOverviews(ListProductOverviewsParam param)
        {
            //
            // TODO: implement a real query
            //
 
            string productViewQuery = connection.From<object>().Select(_ => new
            {
                Id = Guid.Empty,
                Brand = "Samsung",
                Name = "Galaxy Tab A9+",
                Quantity = 10,
                Price = 900,
                ReleaseDateUtc = "10/17/2023",
                MainImage = "main.image"
            }).SelectExpression;

            //
            // Assemble the ProductOverview CTE
            //

            SqlExpression<ProductOverview> queryAgainstCTE = connection
                .From<ProductOverview>()
                .Select()
                .Where(param.Filter)
                .Skip((int) param.Skip)
                .Limit((int) param.Take);

            if (param.SortBy?.Count > 0)
            {
                bool first = true;

                foreach ((Expression<Func<ProductOverview, object>> Property, bool Asc) in param.SortBy)
                {
                    if (first)
                    {
                        if (Asc) queryAgainstCTE.OrderBy(Property); else queryAgainstCTE.OrderByDescending(Property);
                    }
                    else
                    {
                        if (Asc) queryAgainstCTE.ThenBy(Property); else queryAgainstCTE.ThenByDescending(Property);
                    }

                    first = false;
                }
            }

            string finalQuery = $"""
                WITH {dialectProvider.GetQuotedTableName(typeof(ProductOverview))} AS ({productViewQuery})
                {queryAgainstCTE.ToMergedParamsSelectStatement()}
            """;

            return connection.SqlListAsync<ProductOverview>(finalQuery);
        }
    }
}
