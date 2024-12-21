using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Warehouse.DAL
{
    /// <summary>
    /// TODO: implement
    /// </summary>
    internal sealed class WarehouseRepository(IDbConnection connection) : IWarehouseRepository
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

        public Task<List<ProductOverview>> ListProductOverviews(ListProductOverviewsParam param) => Task.FromResult<List<ProductOverview>>
        (
            [
                new ProductOverview
                {
                    Id = Guid.Empty,
                    Brand = "Samsung",
                    Name = "Galaxy Tab A9+",
                    Quantity = 10,
                    Price = 10000,
                    ReleaseDateUtc = new DateTime(2023, 10, 17),
                    MainImage = "main.image"
                }
            ]
        );
    }
}
