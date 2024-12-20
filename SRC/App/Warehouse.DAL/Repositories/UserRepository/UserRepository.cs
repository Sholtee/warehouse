using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using ServiceStack.OrmLite;


namespace Warehouse.DAL
{
    using Core.Auth;
    using Entities;

    internal sealed class UserRepository(IDbConnection connection) : IUserRepository
    {
        private sealed record UserRole(string ClientId, string ClientSecretHash, Roles Role);

        public async Task<bool> CreateUser(CreateUserParam param)
        {
            Guid userId = Guid.NewGuid();

            SqlExpression<User> 
                userExists = connection
                    .From<User>()
                    .Select(static _ => 1)
                    .Where<User>(user => user.ClientId == param.ClientId),
                selectNewUser = connection
                    .From<User>()
                    .Select(_ => new { param.ClientId, param.ClientSecretHash, Id = userId, CreatedUtc = DateTime.UtcNow })
                    .UnsafeWhere($"NOT EXISTS ({userExists.ToMergedParamsSelectStatement()})");

            selectNewUser.FromExpression = " ";

            using IDbTransaction? transaction = connection.OpenTransactionIfNotExists();
            try
            {
                long rowsInserted = await connection.InsertIntoSelectAsync<User>(selectNewUser);
                if (rowsInserted is 0)
                    return false;

                SqlExpression<Group> selectNewUserGroup = connection
                    .From<Group>()
                    .Select<Group>(grp => new { GroupId = grp.Id, UserId = userId, Id = Sql.Custom("UUID()"), CreatedUtc = DateTime.UtcNow })
                    .Where<Group>(grp => Sql.In(grp.Name, param.Groups));

                rowsInserted = await connection.InsertIntoSelectAsync<UserGroup>(selectNewUserGroup);
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

        public async Task<QueryUserResult?> QueryUser(string clientId)
        {
            string sql = connection
                .From<User>()
                .SelectDistinct<User, Role>
                (
                    static (user, role) => new
                    {
                        user.ClientId,
                        user.ClientSecretHash,
                        Role = role.Name
                    }
                )
                .Join<User, UserGroup>(static (user, ug) => user.Id == ug.UserId)
                .Join<UserGroup, GroupRole>(static (ug, gr) => ug.GroupId == gr.GroupId)
                .Join<GroupRole, Role>(static (gr, role) => gr.RoleId == role.Id)
                .Where<User>(user => user.ClientId == clientId && user.DeletedUtc == null)
                .ToMergedParamsSelectStatement();

            List<UserRole> tmp = await connection.SelectAsync<UserRole>(sql);

            return tmp
                .GroupBy(static r => new { r.ClientId, r.ClientSecretHash })
                .Select
                (
                    static grp => new QueryUserResult
                    {
                        ClientId = grp.Key.ClientId,
                        ClientSecretHash = grp.Key.ClientSecretHash,
                        Roles = grp.Aggregate(Roles.None, static (current, role) => current | role.Role)
                    }
                )
                .SingleOrDefault();
        }

        public async Task<bool> DeleteUser(string clientId) => await connection.UpdateAsync<User>
        (
            new { DeletedUtc = DateTime.UtcNow },
            user => user.ClientId == clientId && user.DeletedUtc == null
        ) is 1;
    }
}