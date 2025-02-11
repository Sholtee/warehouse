/********************************************************************************
* ISessionManager.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
namespace Warehouse.Core.Abstractions
{
    /// <summary>
    /// Session manager
    /// </summary>
    public interface ISessionManager
    {
        /// <summary>
        /// Token that represents the current session
        /// </summary>
        /// <remarks>Setting the value to null will remove the session from the user context</remarks>
        string? Token { get; set; }

        /// <summary>
        /// Determines if the system should refresh the token after the validation
        /// </summary>
        bool SlidingExpiration { get; }
    }
}
