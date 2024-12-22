/********************************************************************************
* ProductDetails.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
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

        /// <summary>
        /// Overall rating. Null if no one rated the product yet.
        /// </summary>
        public float? Rating { get; init; }

        /// <summary>
        /// Image URLs associated with this product.
        /// </summary>
        public required IReadOnlyList<string> Images { get; init; }
    }
}
