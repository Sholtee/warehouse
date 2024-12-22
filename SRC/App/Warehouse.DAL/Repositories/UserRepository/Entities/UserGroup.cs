using System;

using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    [CompositeIndex(nameof(UserId), nameof(GroupId), Unique = true)]
    internal sealed class UserGroup : EntityBase
    {
        [References(typeof(User)), Required]
        public required Guid UserId { get; init; }

        [References(typeof(Group)), Required]
        public required Guid GroupId { get; init; }
    }
}
