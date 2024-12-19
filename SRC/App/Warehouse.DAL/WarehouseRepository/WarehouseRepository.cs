using System.Data;
using System.Threading.Tasks;

using ServiceStack.OrmLite;

namespace Warehouse.DAL
{
    internal sealed class WarehouseRepository(IDbConnection connection) : IWarehouseRepository
    {
        public async Task<bool> IsHealthy() => await connection.SqlScalarAsync<int>("SELECT 1") is 1;
    }
}
