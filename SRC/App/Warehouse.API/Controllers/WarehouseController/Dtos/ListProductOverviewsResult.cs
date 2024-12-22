/********************************************************************************
* ListProductOverviewsResult.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// <see cref="WarehouseController.ListProductOverviews(ListProductOverviewsParam)"/> result.
    /// </summary>
    public sealed class ListProductOverviewsResult
    {
        /// <summary>
        /// Product overview.
        /// </summary>
        public required List<ProductOverview> ProductOverviews { get; init; }
    }
}
