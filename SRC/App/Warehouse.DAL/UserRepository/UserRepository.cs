using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using ServiceStack.OrmLite;


namespace Warehouse.DAL
{
    using Entities;

    public interface IUserRepository
    {
        /// <summary>
        /// Creates a new user entry.
        /// </summary>
        Task<bool> CreateUser(CreateUserParam param);

        /// <summary>
        /// Queries the user associated with the given <paramref name="clientId"/>. Returns null if there is no user with such id
        /// </summary>
        Task<QueryUserResult?> QueryUser(string clientId);

        /// <summary>
        /// Deletes the given user. Removal doesn't mean physical deletion.
        /// </summary>
        Task<bool> DeleteUser(string clientId);
    }

    public sealed class UserRepository(IDbConnection connection) : IUserRepository
    {
        private sealed record UserRole(string ClientId, string ClientSecretHash, string RoleName);

        /// <inheritdoc/>
        public async Task<bool> CreateUser(CreateUserParam param)
        {
            User user = new()
            {
                ClientId = param.ClientId,
                ClientSecretHash = param.ClientSecretHash
            };

            using IDbTransaction? transaction = connection.OpenTransactionIfNotExists();

            try
            {
                long rowsInserted = await connection.InsertIntoSelectAsync<User>
                (
                    connection
                        .From<User>()
                        .Select<User>(_ => user)
                        .WhereNotExists
                        (
                            connection
                                .From<User>()
                                .Select<User>(static _ => 1)
                                .Where<User>(user => user.ClientId == param.ClientId)
                        )
                );
                if (rowsInserted is 0)
                    return false;

                rowsInserted = await connection.InsertIntoSelectAsync<UserGroup>
                (
                    connection
                        .From<Group>()
                        .Select<Group>(grp => new UserGroup { GroupId = grp.Id, UserId = user.Id })
                        .Where<Group>(grp => Sql.In(grp.Name, param.Groups))
                );
                if (rowsInserted != param.Groups.Count)
                    throw new InvalidOperationException("Invalid group");

                transaction?.Commit();
                return true;
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<QueryUserResult?> QueryUser(string clientId)
        {
            string sql = connection
                .From<User>()
                .SelectDistinct<User, Role>(static (user, role) => new { user.ClientId, user.ClientSecretHash, RoleName = role.Name })
                .Join<User, UserGroup>(static (user, ug) => user.Id == ug.UserId)
                .Join<UserGroup, GroupRole>(static (ug, gr) => ug.GroupId == gr.GroupId)
                .Join<GroupRole, Role>(static (gr, role) => gr.RoleId == role.Id)
                .Where<User>(user => user.ClientId == clientId && user.DeletedUtc == null)
                .ToSelectStatement();

            List<UserRole> tmp = await connection.SelectAsync<UserRole>(sql);

            return tmp
                .GroupBy(static r => new { r.ClientId, r.ClientSecretHash })
                .Select
                (
                    static grp => new QueryUserResult
                    {
                        ClientId = grp.Key.ClientId,
                        ClientSecretHash = grp.Key.ClientSecretHash,
                        Roles = grp.Select(static role => role.RoleName).ToList()
                    }
                )
                .SingleOrDefault();
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteUser(string clientId) => await connection.UpdateAsync<User>
        (
            new { DeletedUtc = DateTime.UtcNow },
            user => user.ClientId == clientId && user.DeletedUtc == null
        ) is 1;
    }
}
