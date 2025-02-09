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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Warehouse.DAL
{
    using Extensions;
    using Views;

    /// <summary>
    /// TODO: finish the implementation
    /// </summary>
    internal sealed class WarehouseRepository(IDbConnection connection) : IWarehouseRepository
    {
        public async Task<ProductDetails?> GetProductDetailsById(Guid productId)
        {
            //
            // TODO: implement a real query
            //

            string testData = connection
                .From<object>()
                .Select(static _ => new
                {
                    Id = Guid.Empty,
                    Brand = "Samsung",
                    Name = "Galaxy Tab A9+",
                    Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                    Quantity = 10,
                    Price = 900,
                    ReleaseDateUtc = "10/17/2023",
                    MainImage = "main.png",
                    TagName = "tablets",
                    ImagePath = Sql.Custom("{0}")
                })
                .SelectExpression;

            SqlExpression<ProductDetails> query = connection
                .With<ProductDetails>
                (
                    string.Join
                    (
                        "\nUNION\n",
                        testData.SqlFmt("image1.png"),
                        testData.SqlFmt("image2.png")
                    )
                )
                .Select("*")
                .Where(product => product.Id == productId);

            //
            // Query the actual view
            //

            List<ProductDetails> result = await connection.SelectComposite<ProductDetails, TagView, ImageView>
            (
                query.ToMergedParamsSelectStatement(),
                static product => product.Id,
                static tag => tag.TagName,
                static (product, tag) =>
                {
                    if (!product.Tags.Contains(tag.TagName))
                        product.Tags.Add(tag.TagName);
                },
                static img => img.ImagePath,
                static (product, img) => product.Images.Add(img.ImagePath)
            );

            return result.SingleOrDefault();
        }

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
                MainImage = "main.png"
            }).SelectExpression;

            //
            // Assemble the ProductOverview CTE
            //

            SqlExpression<ProductOverview> queryAgainstCTE = connection
                .With<ProductOverview>(productViewQuery)
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

            return connection.SqlListAsync<ProductOverview>(queryAgainstCTE.ToMergedParamsSelectStatement());
        }
    }
}
