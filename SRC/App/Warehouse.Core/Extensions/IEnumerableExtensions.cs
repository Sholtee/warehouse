/********************************************************************************
* IEnumerableExtensions.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Warehouse.Core.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> self) =>
            self.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue));

        public static IEnumerable<T> Random<T>(this IEnumerable<T> self, int len) =>
            self.Shuffle().Take(len);
    }
}
