using System.Collections.Generic;

namespace Warehouse.DAL
{
    /// <summary>
    /// Describes an user group.
    /// </summary>
    public sealed class CreateGroupParam
    {
        /// <summary>
        /// The group id
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Description
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// The assigned roles
        /// </summary>
        public required IReadOnlyList<string> Roles { get; init; }
    }
}
