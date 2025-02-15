/********************************************************************************
* ITokenManager.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Security.Claims;
using System.Threading.Tasks;


namespace Warehouse.Core.Abstractions
{
    using Auth;

    /// <summary>
    /// Service to manage session tokens
    /// </summary>
    public interface ITokenManager
    {
        /// <summary>
        /// Creates a new token
        /// </summary>
        Task<string> CreateTokenAsync(string user, Roles roles);

        /// <summary>
        /// Refresh the current token by updating its expiration
        /// </summary>
        Task<string> RefreshTokenAsync(string token);

        /// <summary>
        /// Gets the identity associated to the given <paramref name="token"/>. Returns null if the token is invalid.
        /// </summary>
        Task<ClaimsIdentity?> GetIdentityAsync(string token);

        /// <summary>
        /// Revokes the given token.
        /// </summary>
        Task<bool> RevokeTokenAsync(string token);
    }
}
