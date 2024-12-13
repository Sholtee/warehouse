using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities.Auth
{
    internal sealed class User : EntityBase
    {
        [PrimaryKey]
        public required string ClientId { get; init; }

        [Required, StringLength(1024)]
        public required string ClientSecretHash { get; init; }
    }
}
