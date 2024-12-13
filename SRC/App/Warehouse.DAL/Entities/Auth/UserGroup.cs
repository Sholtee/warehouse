using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities.Auth
{
    [CompositeIndex(nameof(ClientId), nameof(GroupId), Unique = true)]
    internal sealed class UserGroup : EntityBase
    {
        [References(typeof(User)), Required]
        public required string ClientId { get; init; }

        [References(typeof(Group)), Required]
        public required string GroupId { get; init; }
    }
}
