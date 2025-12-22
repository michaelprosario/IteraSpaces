using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class LeanTopicRepository : Repository<LeanTopic>, ILeanTopicRepository
{
    private readonly IDocumentStore _documentStore;

    public LeanTopicRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<LeanTopic>> GetBySessionIdAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanTopic>()
            .Where(t => t.LeanSessionId == sessionId && !t.IsDeleted)
            .OrderByDescending(t => t.VoteCount)
            .ThenBy(t => t.DisplayOrder)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanTopic>> GetBySessionIdAndStatusAsync(string sessionId, TopicStatus status)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanTopic>()
            .Where(t => t.LeanSessionId == sessionId && t.Status == status && !t.IsDeleted)
            .OrderByDescending(t => t.VoteCount)
            .ThenBy(t => t.DisplayOrder)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<LeanTopic?> GetCurrentTopicAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<LeanTopic>()
            .FirstOrDefaultAsync(t => 
                t.LeanSessionId == sessionId && 
                t.Status == TopicStatus.Discussing && 
                !t.IsDeleted);
    }
}
