using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class ChallengePhaseRepository : Repository<ChallengePhase>, IChallengePhaseRepository
{
    private readonly IDocumentStore _documentStore;

    public ChallengePhaseRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<ChallengePhase>> GetByChallengeIdAsync(string challengeId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<ChallengePhase>()
            .Where(p => p.ChallengeId == challengeId && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.StartDate)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<ChallengePhase>> SearchAsync(GetChallengePhasesQuery query)
    {
        using var session = _documentStore.QuerySession();
        
        var queryable = session.Query<ChallengePhase>()
            .Where(p => !p.IsDeleted);

        // Apply filters
        if (!string.IsNullOrEmpty(query.ChallengeId))
        {
            queryable = queryable.Where(p => p.ChallengeId == query.ChallengeId);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(p => p.Status == query.Status.Value);
        }

        // Apply sorting by display order and start date
        queryable = queryable
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.StartDate);

        var result = await queryable.ToListAsync();
        return result.ToList();
    }
}
