using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    internal abstract class EntityBase
    {
        [Required]
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

        [Index]
        public DateTime? DeletedUtc { get; init; }
    }
}
