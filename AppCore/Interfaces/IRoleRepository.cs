using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role?> GetByNameAsync(string name);
        Task<List<Role>> GetAllRolesAsync();
        Task<bool> RoleExistsAsync(string name);
    }
}
