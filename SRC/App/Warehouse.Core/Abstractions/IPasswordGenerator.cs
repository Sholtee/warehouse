/********************************************************************************
* IPasswordGenerator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
namespace Warehouse.Core.Abstractions
{
    /// <summary>
    /// Contract of generating password
    /// </summary>
    public interface IPasswordGenerator
    {
        /// <summary>
        /// Generates a new random password.
        /// </summary>
        string Generate(int minLen);
    }
}
