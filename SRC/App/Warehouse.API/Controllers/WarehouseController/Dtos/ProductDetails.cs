using System.Collections.Generic;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// Product details
    /// </summary>
    public sealed class ProductDetails : ProductOverview
    {
        /// <summary>
        /// Product types associated with this item.
        /// </summary>
        public required IReadOnlyList<string> Types { get; init; }

        /// <summary>
        /// Long description of the product.
        /// </summary>
        public required string Description { get; init; }
    }
}
