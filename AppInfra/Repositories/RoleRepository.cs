using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDocumentStore _documentStore;

        public RoleRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<Role?> GetById(string id)
        {
            using var session = _documentStore.LightweightSession();
            return await session.LoadAsync<Role>(id);
        }

        public async Task<Role> Add(Role entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Store(entity);
            await session.SaveChangesAsync();
            return entity;
        }

        public async Task Update(Role entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Update(entity);
            await session.SaveChangesAsync();
        }

        public async Task Delete(Role entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            await Update(entity);
        }

        public async Task<bool> RecordExists(string id)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<Role>().AnyAsync(r => r.Id == id);
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<Role>()
                .FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            using var session = _documentStore.QuerySession();
            var roles = await session.Query<Role>().ToListAsync();
            return roles.ToList();
        }

        public async Task<bool> RoleExistsAsync(string name)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<Role>()
                .AnyAsync(r => r.Name == name);
        }
    }
}
