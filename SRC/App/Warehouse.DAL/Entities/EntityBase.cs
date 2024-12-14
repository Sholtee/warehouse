using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    internal abstract class EntityBase
    {
        [PrimaryKey]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Required]
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        [Index]
        public DateTime? DeletedUtc { get; init; }
    }
}
