using System.Collections.Generic;

namespace Warehouse.DAL
{
    /// <summary>
    /// Describes an user
    /// </summary>
    public sealed class CreateUserParam
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
        /// Groups associated to this user
        /// </summary>
        public required IReadOnlyList<string> Groups { get; init; }
    }
}
