/********************************************************************************
* DapperExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using ServiceStack.OrmLite.Dapper;

namespace Warehouse.DAL
{
    internal static class DapperExtensions
    {
        private sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
        {
            public override void SetValue(IDbDataParameter parameter, Guid guid) => parameter.Value = guid.ToString();

            public override Guid Parse(object value) => Guid.Parse((string)value);
        }

        public static void ExtendMappers()
        {
            SqlMapper.AddTypeHandler(new GuidTypeHandler());
        }

        public static async Task<List<TParent>> SelectComposite<TParent, TNested1, TNested2>
        (
            this IDbConnection connection,
            string query,
            Func<TParent, Guid> idSelector,
            Expression<Func<TNested1, object>> splitOn1,
            Action<TParent, TNested1> addNested1,
            Expression<Func<TNested2, object>> splitOn2,
            Action<TParent, TNested2> addNested2
        )
        {
            Dictionary<Guid, TParent> processed = [];

            IEnumerable<TParent> result = await connection.QueryAsync<TParent, TNested1, TNested2, TParent>
            (
                query,
                (parent, nested1, nested2) =>
                {
                    if (!processed.TryGetValue(idSelector(parent), out TParent? existing))
                    {
                        existing = parent;
                        processed.Add(idSelector(parent), parent);
                    }

                    if (nested1 is not null)
                        addNested1(existing, nested1);
  
                    if (nested2 is not null)
                        addNested2(existing, nested2);

                    return existing;
                },
                splitOn: $"{GetPropertyName(splitOn1)},{GetPropertyName(splitOn2)}"
            );

            return result.Distinct().ToList();

            static string GetPropertyName(LambdaExpression expr) => ((MemberExpression) expr.Body).Member.Name;
        }
    }
}
