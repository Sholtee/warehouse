using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ServiceStack.OrmLite;


namespace Warehouse.DAL.Tests
{
    using Core.Auth;

    [TestFixture]
    internal class UserRepositoryTests
    {
        private const string 
            TEST_USER = "test_user",
            TEST_USER_2 = "test_user_2";

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
                    new CreateGroupParam { Name = "Admins", Roles = Roles.Admin | Roles.User },
                    new CreateGroupParam { Name = "Users",  Roles = Roles.User }
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
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER, ClientSecretHash = "hash", Groups = ["Admins", "Users"] }), Is.True);

            User queried = (await _userRepository.QueryUser(TEST_USER))!;
            
            Assert.Multiple(() =>
            {
                Assert.That(queried, Is.Not.Null);
                Assert.That(queried.ClientId, Is.EqualTo(TEST_USER));
                Assert.That(queried.ClientSecretHash, Is.EqualTo("hash"));
                Assert.That(queried.Roles, Is.EqualTo(Roles.User | Roles.Admin));
            });
        }

        [Test]
        public async Task CreateUser_ShouldCreateANewUser_Mutiple()
        {
            await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER_2, ClientSecretHash = "hash", Groups = ["Users"] });
            await CreateUser_ShouldCreateANewUser();
        }

        [Test]
        public void CreateUser_ShouldReturnFalseIfTheUserAlreadyExists() => Assert.MultipleAsync(async () =>
        {
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER, ClientSecretHash = "hash", Groups = ["Admins", "Users"] }));
            Assert.That(await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER, ClientSecretHash = "hash2", Groups = ["Users"] }), Is.False);
        });

        [Test]
        public Task CreateUser_ShouldThrowOnInvalidGroup() => Assert.MultipleAsync(async () =>
        {
            InvalidOperationException exc = Assert.ThrowsAsync<InvalidOperationException>
            (
                () => _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER, ClientSecretHash = "hash", Groups = ["Invalid"] })
            )!;

            Assert.That(exc.Message, Is.EqualTo("Invalid group"));
            Assert.That(await _userRepository.QueryUser(TEST_USER), Is.Null);
        });

        [Test]
        public async Task DeleteUser_ShouldRemoveTheUser()
        {
            await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER, ClientSecretHash = "hash", Groups = ["Admins", "Users"] });

            Assert.That(await _userRepository.DeleteUser(TEST_USER), Is.True);
            Assert.That(await _userRepository.QueryUser(TEST_USER), Is.Null);
        }

        [Test]
        public async Task DeleteUser_ShouldRemoveTheUser_Multiple()
        {
            await _userRepository.CreateUser(new CreateUserParam { ClientId = TEST_USER_2, ClientSecretHash = "hash", Groups = ["Users"] });
            await DeleteUser_ShouldRemoveTheUser();
        }

        [Test]
        public async Task DeleteUser_ShouldReturnFalseIfTheUserDoesntExist() => Assert.That(await _userRepository.DeleteUser(TEST_USER), Is.False);
    }
}
