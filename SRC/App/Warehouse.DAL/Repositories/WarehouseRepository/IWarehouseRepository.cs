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
    /// TODO: finish design
    /// </summary>
    public interface IWarehouseRepository
    {
        /// <summary>
        /// Returns if the database is healthy
        /// </summary>
        Task<bool> IsHealthy();

        /// <summary>
        /// Queries product overviews by the given criteria
        /// </summary>
        Task<List<ProductOverview>> ListProductOverviews(ListProductOverviewsParam param);

        /// <summary>
        /// Queries the product details.
        /// </summary>
        Task<ProductDetails?> GetProductDetailsById(Guid productId);
    }
}
