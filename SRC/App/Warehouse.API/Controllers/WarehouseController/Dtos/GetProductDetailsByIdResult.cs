/********************************************************************************
* GetProductDetailsByIdResult.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

namespace Warehouse.API.Controllers
{
    /// <summary>
    /// <see cref="WarehouseController.GetProductDetailsById(Guid)"/> result
    /// </summary>
    public sealed class GetProductDetailsByIdResult
    {
        /// <summary>
        /// The product details
        /// </summary>
        public required ProductDetails ProductDetails { get; init; }
    }
}
