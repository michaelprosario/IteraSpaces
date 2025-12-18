using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly IDocumentStore _documentStore;

        public UserRoleRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<UserRole?> GetById(string id)
        {
            using var session = _documentStore.QuerySession();
            var userRole = await session.LoadAsync<UserRole>(id);
                        
            return userRole;
        }

        public async Task<UserRole> Add(UserRole entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Store(entity);
            await session.SaveChangesAsync();
            return entity;
        }

        public async Task Update(UserRole entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Update(entity);
            await session.SaveChangesAsync();
        }

        public async Task Delete(UserRole entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            await Update(entity);
        }

        public async Task<bool> RecordExists(string id)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<UserRole>().AnyAsync(ur => ur.Id == id);
        }

        public async Task<List<UserRole>> GetUserRolesAsync(string userId)
        {
            using var session = _documentStore.QuerySession();
            var userRoles = await session.Query<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
                        
            return userRoles.ToList();
        }

        public async Task<List<string>> GetUserRoleNamesAsync(string userId)
        {
            using var session = _documentStore.QuerySession();
            var userRoles = await session.Query<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
            
            var roleNames = new List<string>();
            foreach (var ur in userRoles)
            {
                var role = await session.LoadAsync<Role>(ur.RoleId);
                if (role != null && !role.IsDeleted)
                {
                    roleNames.Add(role.Name);
                }
            }
            
            return roleNames;
        }

        public async Task<UserRole?> GetUserRoleAsync(string userId, string roleId)
        {
            using var session = _documentStore.QuerySession();
            var userRole = await session.Query<UserRole>()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
                        
            return userRole;
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleId)
        {
            using var session = _documentStore.QuerySession();
            var userRole = await session.Query<UserRole>()
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
            
            if (userRole == null) return false;
            
            var role = await session.LoadAsync<Role>(userRole.RoleId);
            return role != null && !role.IsDeleted;
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleId)
        {
            using var session = _documentStore.QuerySession();
            var userRoles = await session.Query<UserRole>()
                .Where(ur => ur.RoleId == roleId)
                .ToListAsync();
            
            var users = new List<User>();
            foreach (var ur in userRoles)
            {
                var user = await session.LoadAsync<User>(ur.UserId);
                if (user != null && !user.IsDeleted)
                {
                    users.Add(user);
                }
            }
            
            return users;
        }
    }
}
