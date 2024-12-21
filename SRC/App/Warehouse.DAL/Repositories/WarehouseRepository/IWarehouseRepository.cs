using System;
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
        Task<ListProductOverviewsResult> ListProductOverviews(ListProductOverviewsParam param);

        /// <summary>
        /// Queries the product details.
        /// </summary>
        Task<GetProductDetailsByIdResult?> GetProductDetailsById(Guid productId);
    }
}
