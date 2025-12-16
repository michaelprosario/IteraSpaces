using System.Linq;
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

        public T? GetById(string id)
        {
            return _context.Set<T>().FirstOrDefault(e => e.Id == id);
        }

        public T Add(T entity)
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
            _context.SaveChanges();
        }

        public void Delete(T entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            Update(entity);
        }

        public bool RecordExists(string id)
        {
            return _context.Set<T>().Any(e => e.Id == id);
        }
    }
}
