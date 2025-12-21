using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class LeanParticipantRepository : Repository<LeanParticipant>, ILeanParticipantRepository
{
    private readonly IDocumentStore _documentStore;

    public LeanParticipantRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<LeanParticipant>> GetBySessionIdAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanParticipant>()
            .Where(p => p.LeanSessionId == sessionId && !p.IsDeleted)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanParticipant>> GetActiveParticipantsBySessionIdAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanParticipant>()
            .Where(p => p.LeanSessionId == sessionId && p.IsActive && !p.IsDeleted)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<LeanParticipant?> GetBySessionAndUserIdAsync(string sessionId, string userId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<LeanParticipant>()
            .FirstOrDefaultAsync(p => 
                p.LeanSessionId == sessionId && 
                p.UserId == userId && 
                !p.IsDeleted);
    }
}
