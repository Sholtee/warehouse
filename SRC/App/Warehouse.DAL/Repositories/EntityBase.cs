/********************************************************************************
* EntityBase.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    internal abstract class EntityBase
    {
        [PrimaryKey]
        public Guid Id { get; init; } = Guid.NewGuid();

        [Required]
        public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
    }
}
