using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDocumentStore _documentStore;

        public UserRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<User?> GetById(string id)
        {
            using var session = _documentStore.LightweightSession();
            return await session.LoadAsync<User>(id);
        }

        public async Task<User> Add(User entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Store(entity);
            await session.SaveChangesAsync();
            return entity;
        }

        public async Task Update(User entity)
        {
            using var session = _documentStore.LightweightSession();
            session.Update(entity);
            await session.SaveChangesAsync();
        }

        public async Task Delete(User entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            await Update(entity);
        }

        public async Task<bool> RecordExists(string id)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<User>().AnyAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<User>()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByFirebaseUidAsync(string firebaseUid)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<User>()
                .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid);
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            using var session = _documentStore.QuerySession();
            var users = await session.Query<User>()
                .Where(u => u.DisplayName.Contains(searchTerm) || 
                           u.Email.Contains(searchTerm))
                .ToListAsync();
            return users.ToList();
        }

        public async Task<List<User>> GetUsersByStatusAsync(UserStatus status)
        {
            using var session = _documentStore.QuerySession();
            var users = await session.Query<User>()
                .Where(u => u.Status == status)
                .ToListAsync();
            return users.ToList();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            using var session = _documentStore.QuerySession();
            return await session.Query<User>()
                .AnyAsync(u => u.Email == email);
        }
    }
}
