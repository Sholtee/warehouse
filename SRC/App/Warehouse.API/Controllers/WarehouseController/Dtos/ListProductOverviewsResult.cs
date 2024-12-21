using System;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// Describes a product overview
    /// </summary>
    public class ProductOverview
    {
        /// <summary>
        /// The internal product id.
        /// </summary>
        public required Guid Id { get; init; }

        /// <summary>
        /// URL of the main image.
        /// </summary>
        public required string MainImage { get; init; }

        /// <summary>
        /// The name of the product
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Brand of the product
        /// </summary>
        public required string Brand { get; init; }

        /// <summary>
        /// Available quantity
        /// </summary>
        public required uint Quantity { get; init; }

        /// <summary>
        /// Price
        /// </summary>
        public required decimal Price { get; init; }

        /// <summary>
        /// Date first available.
        /// </summary>
        public required DateTime ReleaseDate { get; init; }
    }

    /// <summary>
    /// <see cref="WarehouseController.ListProductOverviews(ListProductOverviewsParam)"/> result.
    /// </summary>
    public sealed class ListProductOverviewsResult
    {
        /// <summary>
        /// Product overview.
        /// </summary>
        public required ProductOverview ProductOverview { get; init; }
    }
}
