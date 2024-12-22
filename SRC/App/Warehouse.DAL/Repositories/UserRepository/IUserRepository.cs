using System.Threading.Tasks;


namespace Warehouse.DAL
{
    /// <summary>
    /// Abstract user repository.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Creates a new user entry.
        /// </summary>
        Task<bool> CreateUser(CreateUserParam param);

        /// <summary>
        /// Queries the user associated with the given <paramref name="clientId"/>. Returns null if there is no user with such id
        /// </summary>
        Task<User?> QueryUser(string clientId);

        /// <summary>
        /// Deletes the given user. Removal doesn't mean physical deletion.
        /// </summary>
        Task<bool> DeleteUser(string clientId);
    }
}
