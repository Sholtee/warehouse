using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities.Auth
{
    internal sealed class Group : EntityBase
    {
        [Index(Unique = true)]
        public required string Name { get; init; }

        [StringLength(1024)]
        public string? Description { get; init; }
    }
}
