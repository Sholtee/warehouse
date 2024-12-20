using System.Threading.Tasks;

namespace Warehouse.DAL
{
    /// <summary>
    /// TODO: design
    /// </summary>
    public interface IWarehouseRepository
    {
        /// <summary>
        /// Returns if the database is healthy
        /// </summary>
        Task<bool> IsHealthy();
    }
}
