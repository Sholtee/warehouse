using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities.Auth
{
    internal sealed class Role : EntityBase
    {
        [PrimaryKey]
        public required string RoleId { get; init; }

        [StringLength(1024)]
        public string? Description { get; init; }
    }
}
