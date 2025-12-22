using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IUserDeviceTokenRepository : IRepository<UserDeviceToken>
{
    Task<List<UserDeviceToken>> GetActiveTokensByUserIdAsync(string userId);
    Task<UserDeviceToken?> GetByTokenAsync(string deviceToken);
    Task DeactivateTokenAsync(string deviceToken);
    Task DeactivateUserTokensAsync(string userId);
}
