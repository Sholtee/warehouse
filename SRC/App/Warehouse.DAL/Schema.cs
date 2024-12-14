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
        /// Schema.Dump("Auth");
        /// </code>
        /// </example>
        public static string Dump(string namespaceSuffix)
        {
            IOrmLiteDialectProvider dialectProvider = OrmLiteConfig.DialectProvider;

            StringBuilder sb = new();

            HashSet<Type> processed = [];

            foreach (Type t in typeof(Schema).Assembly.GetTypes())
            {
                if (t.Namespace?.EndsWith($".{namespaceSuffix}") is not true)
                    continue;

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
        /// Dumps the script to initialize the group-role relations.
        /// </summary>
        public static string Dump(params Dtos.Auth.Group[] groups)
        {
            IOrmLiteDialectProvider dialectProvider = OrmLiteConfig.DialectProvider;

            List<string> lines = [];

            IReadOnlyDictionary<string, Guid> roles = groups.SelectMany(static grp => grp.Roles).Distinct().ToDictionary(static role => role, role =>
            {
                Guid id = Guid.NewGuid();

                lines.Add
                (
                    dialectProvider.ToInsertRowSql
                    (
                        new Entities.Auth.Role
                        {
                            Id = id,
                            Name = role
                        }
                    )
                );

                return id;
            });

            foreach (Dtos.Auth.Group group in groups)
            {
                Guid groupId = Guid.NewGuid();

                lines.Add
                (
                    dialectProvider.ToInsertRowSql
                    (
                        new Entities.Auth.Group
                        {
                            Id = groupId,
                            Name = group.Name,
                            Description = group.Description
                        }
                    )
                );

                foreach (string role in group.Roles)
                {
                    lines.Add
                    (
                        dialectProvider.ToInsertRowSql
                        (
                            new Entities.Auth.GroupRole
                            {
                                GroupId = groupId,
                                RoleId = roles[role]
                            }
                        )
                    );
                }
            }

            return string.Join($";{Environment.NewLine}", lines);
        }
    }
}
