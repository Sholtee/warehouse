/********************************************************************************
* GroupRole.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    [CompositeIndex(nameof(GroupId), nameof(RoleId), Unique = true)]
    internal sealed class GroupRole : EntityBase
    {
        [References(typeof(Group)), Required]
        public required Guid GroupId { get; init; }

        [References(typeof(Role)), Required]
        public required Guid RoleId { get; init; }
    }
}
