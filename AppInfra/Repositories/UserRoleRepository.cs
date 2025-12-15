using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using AppInfra.Data;
using Microsoft.EntityFrameworkCore;

namespace AppInfra.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserRole? GetById(string id)
        {
            return _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefault(ur => ur.Id == id);
        }

        public UserRole Add(UserRole entity)
        {
            _context.UserRoles.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public void Update(UserRole entity)
        {
            _context.UserRoles.Update(entity);
            _context.SaveChanges();
        }

        public void Delete(UserRole entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            Update(entity);
        }

        public bool RecordExists(string id)
        {
            return _context.UserRoles.Any(ur => ur.Id == id);
        }

        public async Task<List<UserRole>> GetUserRolesAsync(string userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserRoleNamesAsync(string userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }

        public async Task<UserRole?> GetUserRoleAsync(string userId, string roleId)
        {
            return await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => ur.User)
                .ToListAsync();
        }
    }
}
