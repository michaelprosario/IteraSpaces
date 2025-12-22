using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface ILeanParticipantRepository : IRepository<LeanParticipant>
{
    Task<List<LeanParticipant>> GetBySessionIdAsync(string sessionId);
    Task<List<LeanParticipant>> GetActiveParticipantsBySessionIdAsync(string sessionId);
    Task<LeanParticipant?> GetBySessionAndUserIdAsync(string sessionId, string userId);
    Task<IEnumerable<LeanParticipant>> GetActiveParticipantsBySessionAsync(string sessionId);
}
