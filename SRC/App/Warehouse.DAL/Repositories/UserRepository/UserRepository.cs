/********************************************************************************
* UserRepository.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

using ServiceStack.OrmLite;


namespace Warehouse.DAL
{
    using GroupEntity = Entities.Group;
    using UserEntity = Entities.User;
    using UserGroupEntity = Entities.UserGroup;

    internal sealed class UserRepository(IDbConnection connection, IOrmLiteDialectProvider dialectProvider) : IUserRepository
    {
        public async Task<bool> CreateUser(CreateUserParam param)
        {
            Guid userId = Guid.NewGuid();

            SqlExpression<UserEntity> 
                userExists = connection
                    .From<UserEntity>()
                    .Select(static _ => 1)
                    .Where<UserEntity>(user => user.ClientId == param.ClientId),
                selectNewUser = connection
                    .From<UserEntity>(static expr => expr.FromExpression = " ")
                    .Select
                    (
                        _ => new
                        {
                            param.ClientId,
                            param.ClientSecretHash,
                            Id = userId,
                            CreatedUtc = DateTime.UtcNow 
                        }
                    )
                    .UnsafeWhere($"NOT EXISTS ({userExists.ToMergedParamsSelectStatement()})");

            #pragma warning disable CA2000 // false positive, Dispose() is being called on the transaction
            using IDbTransaction? transaction = connection.OpenTransactionIfNotExists();
            #pragma warning restore CA2000

            try
            {
                long rowsInserted = await connection.InsertIntoSelectAsync<UserEntity>(selectNewUser);
                if (rowsInserted is 0)
                    return false;

                SqlExpression<GroupEntity> selectNewUserGroup = connection
                    .From<GroupEntity>()
                    .Select<GroupEntity>
                    (
                        grp => new
                        {
                            GroupId = grp.Id,
                            UserId = userId,
                            Id = Sql.Custom("UUID()"),
                            CreatedUtc = DateTime.UtcNow
                        }
                    )
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
                .Select<UserEntity>
                (
                    user => new
                    {
                        user.ClientId,
                        user.ClientSecretHash,
                        Roles = Sql.Custom($"BIT_OR({
                            typeof(GroupEntity)
                                .GetModelMetadata()
                                .GetFieldDefinition<GroupEntity>(static group => group.Roles)
                                .GetQuotedName(dialectProvider)
                        })")
                    }
                )
                .Join<UserEntity, UserGroupEntity>(static (user, ug) => user.Id == ug.UserId)
                .Join<UserGroupEntity, GroupEntity>(static (ug, gr) => ug.GroupId == gr.Id)
                .GroupBy<UserEntity>
                (
                    static user => new
                    {
                        user.ClientId,
                        user.ClientSecretHash
                    }
                )
                .Where<UserEntity>(user => user.ClientId == clientId && user.DeletedUtc == null)
                .ToMergedParamsSelectStatement();

            List<User> result = await connection.SelectAsync<User>(sql);
            return result.SingleOrDefault();
        }

        public async Task<bool> DeleteUser(string clientId) => await connection.UpdateAsync<UserEntity>
        (
            new { DeletedUtc = DateTime.UtcNow },
            user => user.ClientId == clientId && user.DeletedUtc == null
        ) is 1;
    }
}
