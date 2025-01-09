/********************************************************************************
* IDbConnectionExtensions.cs                                                    *
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

using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Dapper;

namespace Warehouse.DAL.Extensions
{
    internal static class IDbConnectionExtensions
    {
        /*
        public static SqlExpression<TTempTable> With<TTempTable, TFrom>(this IDbConnection connection, Action<SqlExpression<TFrom>> tempTableQuery)
        {
            SqlExpression<TFrom> from = connection.From<TFrom>();
            tempTableQuery(from);
            return connection.With<TTempTable>(from.ToMergedParamsSelectStatement());
        }
        */
        public static SqlExpression<TTempTable> With<TTempTable>(this IDbConnection connection, string tempTableQuery) => connection
            .From<TTempTable>()
            .WithSqlFilter
            (
                sql =>
                $"""
                    WITH {connection.GetDialectProvider().GetQuotedTableName(typeof(TTempTable))} AS ({tempTableQuery})
                    {sql}
                """
            );

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

            static string GetPropertyName(LambdaExpression expr) => ((MemberExpression)expr.Body).Member.Name;
        }
    }
}
