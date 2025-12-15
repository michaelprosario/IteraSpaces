using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces
{
    public interface IUserRoleRepository : IRepository<UserRole>
    {
        Task<List<UserRole>> GetUserRolesAsync(string userId);
        Task<List<string>> GetUserRoleNamesAsync(string userId);
        Task<UserRole?> GetUserRoleAsync(string userId, string roleId);
        Task<bool> UserHasRoleAsync(string userId, string roleId);
        Task<List<User>> GetUsersInRoleAsync(string roleId);
    }
}
