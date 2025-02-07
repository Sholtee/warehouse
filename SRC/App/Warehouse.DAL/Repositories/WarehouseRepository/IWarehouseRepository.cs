/********************************************************************************
* IWarehouseRepository.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Warehouse.DAL
{
    /// <summary>
    /// TODO: finish the design
    /// </summary>
    public interface IWarehouseRepository
    {
        /// <summary>
        /// Queries product overviews by the given criteria
        /// </summary>
        Task<List<ProductOverview>> ListProductOverviews(ListProductOverviewsParam param);

        /// <summary>
        /// Queries detailed product information.
        /// </summary>
        Task<ProductDetails?> GetProductDetailsById(Guid productId);
    }
}
