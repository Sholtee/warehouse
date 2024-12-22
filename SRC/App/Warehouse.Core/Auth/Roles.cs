/********************************************************************************
* Roles.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

namespace Warehouse.Core.Auth
{
    [Flags]
    public enum Roles
    {
        None = 0,
        User = 1 << 0,
        Admin = 1 << 1
    }
}
