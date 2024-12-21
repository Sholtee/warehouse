using System.Collections.Generic;

namespace Warehouse.DAL
{
    public sealed class ProductDetails : ProductOverview
    {
        public required List<string> Types { get; init; }

        public required string Description { get; init; }

        public float? Rating { get; init; }

        public required List<string> Images { get; init; }
    }

    public sealed class GetProductDetailsByIdResult
    {
        public required ProductDetails ProductDetails { get; init; }
    }
}
