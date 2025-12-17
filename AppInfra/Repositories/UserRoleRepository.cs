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

        public async Task<UserRole?> GetById(string id)
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.Id == id);
        }

        public async Task<UserRole> Add(UserRole entity)
        {
            await _context.UserRoles.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task Update(UserRole entity)
        {
            _context.UserRoles.Update(entity);
            await _context.SaveChangesAsync();
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
            return await _context.UserRoles.AnyAsync(ur => ur.Id == id);
        }

        public async Task<List<UserRole>> GetUserRolesAsync(string userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && !ur.Role.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<string>> GetUserRoleNamesAsync(string userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId && !ur.Role.IsDeleted)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
        }

        public async Task<UserRole?> GetUserRoleAsync(string userId, string roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && 
                                          !ur.User.IsDeleted && !ur.Role.IsDeleted);
        }

        public async Task<bool> UserHasRoleAsync(string userId, string roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && !ur.Role.IsDeleted);
        }

        public async Task<List<User>> GetUsersInRoleAsync(string roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId && !ur.User.IsDeleted)
                .Select(ur => ur.User)
                .ToListAsync();
        }
    }
}
