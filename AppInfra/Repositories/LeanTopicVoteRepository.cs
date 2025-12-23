using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class LeanTopicVoteRepository : Repository<LeanTopicVote>, ILeanTopicVoteRepository
{
    private readonly IDocumentStore _documentStore;

    public LeanTopicVoteRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<LeanTopicVote>> GetByTopicIdAsync(string topicId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanTopicVote>()
            .Where(v => v.LeanTopicId == topicId && !v.IsDeleted)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanTopicVote>> GetBySessionIdAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanTopicVote>()
            .Where(v => v.LeanSessionId == sessionId && !v.IsDeleted)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<LeanTopicVote?> GetByTopicAndUserIdAsync(string topicId, string userId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<LeanTopicVote>()
            .FirstOrDefaultAsync(v => 
                v.LeanTopicId == topicId && 
                v.UserId == userId && 
                !v.IsDeleted);
    }

    public async Task<int> GetVoteCountForTopicAsync(string topicId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<LeanTopicVote>()
            .CountAsync(v => v.LeanTopicId == topicId && !v.IsDeleted);
    }

    public async Task<IEnumerable<LeanTopicVote>> GetBySessionAndUserIdAsync(string sessionId, string userId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<LeanTopicVote>()
            .Where(v => v.LeanSessionId == sessionId && v.UserId == userId && !v.IsDeleted)
            .ToListAsync();
    }
}
