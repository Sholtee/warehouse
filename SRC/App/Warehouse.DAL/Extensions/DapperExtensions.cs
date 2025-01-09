/********************************************************************************
* DapperExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Data;

using ServiceStack.OrmLite.Dapper;

namespace Warehouse.DAL.Extensions
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
    }
}
