/********************************************************************************
* EnumExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Warehouse.Core.Extensions
{
    public static class EnumExtensions
    {
        public static IEnumerable<TEnum> SetFlags<TEnum>(this TEnum self) where TEnum: struct, Enum
        {
            foreach (TEnum flag in Enum.GetValues<TEnum>())
                if (!flag.Equals(default(TEnum)) && self.HasFlag(flag))
                    yield return flag;
        }
    }
}
