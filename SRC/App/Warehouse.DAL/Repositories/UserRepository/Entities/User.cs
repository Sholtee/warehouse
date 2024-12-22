/********************************************************************************
* User.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using ServiceStack.DataAnnotations;

namespace Warehouse.DAL.Entities
{
    internal sealed class User : EntityBase
    {
        [Index(Unique = true)]
        public required string ClientId { get; init; }

        [Required, StringLength(1024)]
        public required string ClientSecretHash { get; init; }

        [Index]
        public DateTime? DeletedUtc { get; init; }
    }
}
