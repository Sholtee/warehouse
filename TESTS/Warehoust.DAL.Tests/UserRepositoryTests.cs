using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ServiceStack.OrmLite;


namespace Warehouse.DAL.Tests
{
    [TestFixture]
    internal class UserRepositoryTests
    {
        private IDbConnection _connection = null!;

        private IUserRepository _userRepository = null!;

        [SetUp]
        public void SetupTest()
        {
            SqliteConnection connection = new("DataSource=:memory:");
            connection.Open();

            OrmLiteConfig.DialectProvider = SqliteDialect.Provider;

            StringBuilder schemaSetup = new();
            schemaSetup.AppendLine
            (
                Schema.Dump()
            );
            schemaSetup.AppendLine
            (
                Schema.Dump
                (
                    new CreateGroupParam { Name = "Admins", Roles = ["Admin", "User"] },
                    new CreateGroupParam { Name = "Users",  Roles = ["User"] }
                )
            );

            connection.CreateFunction("UUID", Guid.NewGuid);
            connection.ExecuteNonQuery(schemaSetup.ToString());

            _userRepository = new UserRepository(_connection = connection);
        }

        [TearDown]
        public void TearDownTest()
        {
            _connection.Dispose();
            _connection = null!;

            _userRepository = null!;
        }

        [Test]
        public async Task CreateUser_ShouldCreateANewUser()
        {
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = "test_user", ClientSecretHash = "hash", Groups = ["Admins", "Users"] }));

            QueryUserResult queried = (await _userRepository.QueryUser("test_user"))!;
            
            Assert.Multiple(() =>
            {
                Assert.That(queried, Is.Not.Null);
                Assert.That(queried.ClientId, Is.EqualTo("test_user"));
                Assert.That(queried.ClientSecretHash, Is.EqualTo("hash"));
                Assert.That(queried.Roles, Is.EquivalentTo(["Admin", "User"]));
            });
        }

        [Test]
        public async Task CreateUser_ShouldReturnFalseIfTheUserAlreadyExists()
        {
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = "test_user", ClientSecretHash = "hash", Groups = ["Admins", "Users"] }));
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = "test_user", ClientSecretHash = "hash2", Groups = ["Users"] }), Is.False);
        }

        [Test]
        public async Task CreateUser_ShouldThrowOnInvalidGroup()
        {
            InvalidOperationException exc = Assert.ThrowsAsync<InvalidOperationException>
            (
                () => _userRepository.CreateUser(new CreateUserParam { ClientId = "test_user", ClientSecretHash = "hash", Groups = ["Invalid"] })
            );
            
            Assert.That(exc.Message, Is.EqualTo("Invalid group"));
            Assert.That(await _userRepository.QueryUser("test_user"), Is.Null);
        }
    }
}
