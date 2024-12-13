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

                    sb.AppendLine(MySqlDialect.Instance.ToCreateTableStatement(entity));
                }
            }
        }
    }
}
