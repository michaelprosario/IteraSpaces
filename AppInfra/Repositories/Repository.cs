using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AppCore.Interfaces;
using AppInfra.Data;

namespace AppInfra.Repositories
{
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly ApplicationDbContext _context;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T?> GetById(string id)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<T> Add(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task Update(T entity)
        {
            // Detach any existing tracked entity with the same key
            var existingEntry = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => e.Entity.Id == entity.Id);
            
            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }

            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(T entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            await Update(entity);
        }

        public async Task<bool> RecordExists(string id)
        {
            // Only check for non-deleted records
            return await _context.Set<T>().AnyAsync(e => e.Id == id && !e.IsDeleted);
        }
    }
}
