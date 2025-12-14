using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByFirebaseUidAsync(string firebaseUid);
        Task<List<User>> SearchUsersAsync(string searchTerm);
        Task<List<User>> GetUsersByStatusAsync(UserStatus status);
        Task<bool> EmailExistsAsync(string email);
    }
}
