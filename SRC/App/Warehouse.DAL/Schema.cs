/********************************************************************************
* Schema.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace Warehouse.DAL
{
    using Entities;

    /// <summary>
    /// DB schema related helpers.
    /// </summary>
    public static class Schema
    {
        /// <summary>
        /// Dumps the SQL script to initilize the given schema
        /// </summary>
        /// <example>
        /// <code>
        /// Schema.Dump();
        /// </code>
        /// </example>
        public static string Dump()
        {
            IOrmLiteDialectProvider dialectProvider = OrmLiteConfig.DialectProvider;

            StringBuilder sb = new();

            HashSet<Type> processed = [];

            foreach (Type t in typeof(Schema).Assembly.GetTypes())
            {
                if (t.BaseType != typeof(EntityBase))
                    continue;

                ProcessEntity(t);
            }

            return sb.ToString();

            void ProcessEntity(Type entity)
            {
                if (processed.Add(entity))
                {
                    foreach (PropertyInfo prop in entity.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (prop.GetCustomAttribute<IgnoreAttribute>() is not null)
                            continue;

                        ReferencesAttribute? references = prop.GetCustomAttribute<ReferencesAttribute>();
                        if (references is null)
                            continue;

                        ProcessEntity(references.Type);
                    }

                    sb.AppendLine(dialectProvider.ToCreateTableStatement(entity));
                    sb.AppendLine(string.Join("\n", dialectProvider.ToCreateIndexStatements(entity)));
                }
            }
        }

        /// <summary>
        /// Dumps the script to initialize the group entities.
        /// </summary>
        public static string Dump(params CreateGroupParam[] groups)
        {
            ArgumentNullException.ThrowIfNull(groups, nameof(groups));

            return OrmLiteConfig.DialectProvider.ToInsertRowsSql
            (
                groups.Select
                (
                    static grp => new Group
                    {
                        Name = grp.Name,
                        Description = grp.Description,
                        Roles = grp.Roles
                    }
                )
            );
        }
    }
}
