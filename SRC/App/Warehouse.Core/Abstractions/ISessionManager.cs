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
        /// Gets or sets the token that represents the current session
        /// </summary>
        /// <remarks>Setting the value to null will remove the session from the user context</remarks>
        string? Token { get; set; }
    }
}
