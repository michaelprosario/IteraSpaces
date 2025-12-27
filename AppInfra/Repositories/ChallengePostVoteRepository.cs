using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class ChallengePostVoteRepository : Repository<ChallengePostVote>, IChallengePostVoteRepository
{
    private readonly IDocumentStore _documentStore;

    public ChallengePostVoteRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<ChallengePostVote?> GetVoteAsync(string challengePostId, string userId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<ChallengePostVote>()
            .FirstOrDefaultAsync(v => 
                v.ChallengePostId == challengePostId && 
                v.UserId == userId && 
                !v.IsDeleted);
    }

    public async Task<bool> HasUserVotedAsync(string challengePostId, string userId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<ChallengePostVote>()
            .AnyAsync(v => 
                v.ChallengePostId == challengePostId && 
                v.UserId == userId && 
                !v.IsDeleted);
    }

    public async Task<int> GetVoteCountAsync(string challengePostId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<ChallengePostVote>()
            .CountAsync(v => v.ChallengePostId == challengePostId && !v.IsDeleted);
    }
}
