/********************************************************************************
* ITokenManager.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

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
        /// Validates the given token.
        /// </summary>
        Task<TokenValidationResult> ValidateTokenAsync(string token);

        /// <summary>
        /// Revokes the given token.
        /// </summary>
        Task RevokeTokenAsync(string token);
    }
}
