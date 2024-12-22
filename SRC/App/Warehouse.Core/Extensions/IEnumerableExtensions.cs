using System;
using System.Collections.Generic;
using System.Linq;

namespace Warehouse.Core.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> self)
        {
            Random random = new();
            return self.OrderBy(_ => random.Next());
        }

        public static IEnumerable<T> Random<T>(this IEnumerable<T> self, int len) =>
            self.Shuffle().Take(len);
    }
}
