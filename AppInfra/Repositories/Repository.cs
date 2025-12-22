using System.Linq;
using System.Threading.Tasks;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories
{
    public class Repository<T> : IRepository<T> where T : class, IEntity
    {
        private readonly IDocumentStore _documentStore;

        public Repository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<T?> GetById(string id)
        {
            using var session = _documentStore.LightweightSession();
            return await session.LoadAsync<T>(id);
        }

        public async Task<T> Add(T entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Store(entity);
            await session.SaveChangesAsync();
            return entity;
        }

        public async Task Update(T entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Update(entity);
            await session.SaveChangesAsync();
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
            using var session = _documentStore.QuerySession();
            // Only check for non-deleted records
            return await session.Query<T>()
                .AnyAsync(e => e.Id == id && !e.IsDeleted);
        }
    }
}
