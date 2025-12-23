using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly IDocumentSession _session;

    public UserDeviceTokenRepository(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<List<UserDeviceToken>> GetActiveTokensByUserIdAsync(string userId)
    {
        var result = await _session.Query<UserDeviceToken>()
            .Where(t => t.UserId == userId && t.IsActive)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<UserDeviceToken?> GetByTokenAsync(string deviceToken)
    {
        return await _session.Query<UserDeviceToken>()
            .FirstOrDefaultAsync(t => t.DeviceToken == deviceToken);
    }

    public async Task DeactivateTokenAsync(string deviceToken)
    {
        var token = await GetByTokenAsync(deviceToken);
        if (token != null)
        {
            token.IsActive = false;
            token.UpdatedAt = DateTime.UtcNow;
            _session.Update(token);
            await _session.SaveChangesAsync();
        }
    }

    public async Task DeactivateUserTokensAsync(string userId)
    {
        var tokens = await GetActiveTokensByUserIdAsync(userId);
        foreach (var token in tokens)
        {
            token.IsActive = false;
            token.UpdatedAt = DateTime.UtcNow;
            _session.Update(token);
        }
        await _session.SaveChangesAsync();
    }

    // IRepository<UserDeviceToken> implementation
    public async Task<UserDeviceToken> Add(UserDeviceToken entity)
    {
        _session.Store(entity);
        await _session.SaveChangesAsync();
        return entity;
    }

    public async Task<UserDeviceToken?> GetById(string id)
    {
        return await _session.LoadAsync<UserDeviceToken>(id);
    }

    public async Task<List<UserDeviceToken>> GetAll()
    {
        var result = await _session.Query<UserDeviceToken>().ToListAsync();
        return result.ToList();
    }

    public async Task Update(UserDeviceToken entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _session.Update(entity);
        await _session.SaveChangesAsync();
    }

    public async Task Delete(UserDeviceToken entity)
    {
        _session.Delete(entity);
        await _session.SaveChangesAsync();
    }

    public async Task<bool> RecordExists(string id)
    {
        var entity = await GetById(id);
        return entity != null;
    }
}
