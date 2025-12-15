using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using AppInfra.Data;
using Microsoft.EntityFrameworkCore;

namespace AppInfra.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Role? GetById(string id)
        {
            return _context.Roles.FirstOrDefault(r => r.Id == id);
        }

        public Role Add(Role entity)
        {
            _context.Roles.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public void Update(Role entity)
        {
            _context.Roles.Update(entity);
            _context.SaveChanges();
        }

        public void Delete(Role entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            Update(entity);
        }

        public bool RecordExists(string id)
        {
            return _context.Roles.Any(r => r.Id == id);
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<bool> RoleExistsAsync(string name)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name == name);
        }
    }
}
