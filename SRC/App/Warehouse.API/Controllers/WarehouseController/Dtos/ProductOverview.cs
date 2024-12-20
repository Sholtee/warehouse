using System;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// Describes a product overview
    /// </summary>
    public class ProductOverview
    {
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
}
