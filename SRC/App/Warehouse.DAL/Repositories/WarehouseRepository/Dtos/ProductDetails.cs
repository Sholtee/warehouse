/********************************************************************************
* ProductDetails.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;

namespace Warehouse.DAL
{
    public sealed class ProductDetails: ProductOverview
    {
        public required List<string> Types { get; init; }

        public required string Description { get; init; }

        public float? Rating { get; init; }

        public required List<string> Images { get; init; }
    }
}
