using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppInfra.Repositories
{
    public class UserRepository : IUserRepository
    {
        // TODO: Replace with actual database context when available
        // For now, using in-memory storage for demonstration
        private static readonly List<User> _users = new List<User>();

        public User? GetById(string id)
        {
            return _users.FirstOrDefault(u => u.Id == id && !u.IsDeleted);
        }

        public User Add(User entity)
        {
            _users.Add(entity);
            return entity;
        }

        public void Update(User entity)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == entity.Id);
            if (existingUser != null)
            {
                var index = _users.IndexOf(existingUser);
                _users[index] = entity;
            }
        }

        public void Delete(User entity)
        {
            // Soft delete
            entity.IsDeleted = true;
            entity.DeletedAt = System.DateTime.UtcNow;
            Update(entity);
        }

        public bool RecordExists(string id)
        {
            return _users.Any(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.Email == email && !u.IsDeleted));
        }

        public async Task<User?> GetByFirebaseUidAsync(string firebaseUid)
        {
            return await Task.FromResult(_users.FirstOrDefault(u => u.FirebaseUid == firebaseUid && !u.IsDeleted));
        }

        public async Task<List<User>> SearchUsersAsync(string searchTerm)
        {
            return await Task.FromResult(_users
                .Where(u => !u.IsDeleted && 
                       (u.DisplayName.Contains(searchTerm) || 
                        u.Email.Contains(searchTerm)))
                .ToList());
        }

        public async Task<List<User>> GetUsersByStatusAsync(UserStatus status)
        {
            return await Task.FromResult(_users
                .Where(u => u.Status == status && !u.IsDeleted)
                .ToList());
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await Task.FromResult(_users.Any(u => u.Email == email && !u.IsDeleted));
        }
    }
}
