/********************************************************************************
* Group.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    internal sealed class Group : EntityBase
    {
        [Index(Unique = true)]
        public required string Name { get; init; }

        [StringLength(1024)]
        public string? Description { get; init; }
    }
}
