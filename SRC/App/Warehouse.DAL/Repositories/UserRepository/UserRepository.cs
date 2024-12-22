using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using ServiceStack.OrmLite;


namespace Warehouse.DAL
{
    using Core.Auth;

    using GroupEntity = Entities.Group;
    using GroupRoleEntity = Entities.GroupRole;
    using RoleEntity = Entities.Role;
    using UserEntity = Entities.User;
    using UserGroupEntity = Entities.UserGroup;

    internal sealed class UserRepository(IDbConnection connection) : IUserRepository
    {
        private sealed record UserRoleView(string ClientId, string ClientSecretHash, Roles Role);

        public async Task<bool> CreateUser(CreateUserParam param)
        {
            Guid userId = Guid.NewGuid();

            SqlExpression<UserEntity> 
                userExists = connection
                    .From<UserEntity>()
                    .Select(static _ => 1)
                    .Where<UserEntity>(user => user.ClientId == param.ClientId),
                selectNewUser = connection
                    .From<UserEntity>()
                    .Select(_ => new { param.ClientId, param.ClientSecretHash, Id = userId, CreatedUtc = DateTime.UtcNow })
                    .UnsafeWhere($"NOT EXISTS ({userExists.ToMergedParamsSelectStatement()})");

            selectNewUser.FromExpression = " ";

            using IDbTransaction? transaction = connection.OpenTransactionIfNotExists();
            try
            {
                long rowsInserted = await connection.InsertIntoSelectAsync<UserEntity>(selectNewUser);
                if (rowsInserted is 0)
                    return false;

                SqlExpression<GroupEntity> selectNewUserGroup = connection
                    .From<GroupEntity>()
                    .Select<GroupEntity>(grp => new { GroupId = grp.Id, UserId = userId, Id = Sql.Custom("UUID()"), CreatedUtc = DateTime.UtcNow })
                    .Where<GroupEntity>(grp => Sql.In(grp.Name, param.Groups));

                rowsInserted = await connection.InsertIntoSelectAsync<UserGroupEntity>(selectNewUserGroup);
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

        public async Task<User?> QueryUser(string clientId)
        {
            string sql = connection
                .From<UserEntity>()
                .SelectDistinct<UserEntity, RoleEntity>
                (
                    static (user, role) => new
                    {
                        user.ClientId,
                        user.ClientSecretHash,
                        Role = role.Name
                    }
                )
                .Join<UserEntity, UserGroupEntity>(static (user, ug) => user.Id == ug.UserId)
                .Join<UserGroupEntity, GroupRoleEntity>(static (ug, gr) => ug.GroupId == gr.GroupId)
                .Join<GroupRoleEntity, RoleEntity>(static (gr, role) => gr.RoleId == role.Id)
                .Where<UserEntity>(user => user.ClientId == clientId && user.DeletedUtc == null)
                .ToMergedParamsSelectStatement();

            List<UserRoleView> tmp = await connection.SelectAsync<UserRoleView>(sql);

            return tmp
                .GroupBy(static r => new { r.ClientId, r.ClientSecretHash })
                .Select
                (
                    static grp => new User
                    {
                        ClientId = grp.Key.ClientId,
                        ClientSecretHash = grp.Key.ClientSecretHash,
                        Roles = grp.Aggregate(Roles.None, static (current, role) => current | role.Role)
                    }
                )
                .SingleOrDefault();
        }

        public async Task<bool> DeleteUser(string clientId) => await connection.UpdateAsync<UserEntity>
        (
            new { DeletedUtc = DateTime.UtcNow },
            user => user.ClientId == clientId && user.DeletedUtc == null
        ) is 1;
    }
}
