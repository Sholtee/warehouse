using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities.Auth
{
    internal sealed class Group : EntityBase
    {
        [PrimaryKey]
        public required string GroupId { get; init; }

        [StringLength(1024)]
        public string? Description { get; init; }
    }
}
