using System.Collections.Generic;

namespace Warehouse.DAL
{
    /// <summary>
    /// Describes an user
    /// </summary>
    public sealed class QueryUserResult
    {
        /// <summary>
        /// Client id
        /// </summary>
        public required string ClientId { get; init; }

        /// <summary>
        /// Client secret hash
        /// </summary>
        public required string ClientSecretHash { get; init; }

        /// <summary>
        /// Roles associated to this user
        /// </summary>
        public required IReadOnlyList<string> Roles { get; init; }
    }
}
