using System;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using Marten;

namespace AppInfra.Repositories;

public class ChallengePostRepository : Repository<ChallengePost>, IChallengePostRepository
{
    private readonly IDocumentStore _documentStore;

    public ChallengePostRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<PagedResults<ChallengePost>> SearchAsync(GetChallengePostsQuery query)
    {
        using var session = _documentStore.QuerySession();
        
        var queryable = session.Query<ChallengePost>()
            .Where(p => !p.IsDeleted);

        // Apply filters
        if (!string.IsNullOrEmpty(query.ChallengePhaseId))
        {
            queryable = queryable.Where(p => p.ChallengePhaseId == query.ChallengePhaseId);
        }

        if (!string.IsNullOrEmpty(query.SubmittedByUserId))
        {
            queryable = queryable.Where(p => p.SubmittedByUserId == query.SubmittedByUserId);
        }

        // Apply tag filter if provided
        if (query.Tags != null && query.Tags.Any())
        {
            foreach (var tag in query.Tags)
            {
                queryable = queryable.Where(p => p.Tags.Contains(tag));
            }
        }

        // Apply search term filter if provided
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            queryable = queryable.Where(p => 
                p.Title.Contains(query.SearchTerm) || 
                p.Description.Contains(query.SearchTerm));
        }

        // Get total count before pagination
        var totalRecords = await queryable.CountAsync();

        // Apply sorting
        var sortBy = query.SortBy?.ToLower() ?? "votes";
        queryable = sortBy switch
        {
            "recent" => queryable.OrderByDescending(p => p.CreatedAt),
            "comments" => queryable.OrderByDescending(p => p.CommentCount).ThenByDescending(p => p.CreatedAt),
            "votes" or _ => queryable.OrderByDescending(p => p.VoteCount).ThenByDescending(p => p.CreatedAt)
        };

        // Apply pagination
        var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
        var pageSize = query.PageSize > 0 ? query.PageSize : 10;
        
        queryable = queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await queryable.ToListAsync();
        var itemsList = items.ToList();

        return new PagedResults<ChallengePost>
        {
            Data = itemsList,
            TotalCount = totalRecords,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            Success = true,
            Message = "Challenge posts retrieved successfully"
        };
    }

    public async Task<int> GetPostCountByPhaseIdAsync(string challengePhaseId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<ChallengePost>()
            .CountAsync(p => p.ChallengePhaseId == challengePhaseId && !p.IsDeleted);
    }

    public async Task<int> GetPostCountByChallengeIdAsync(string challengeId)
    {
        using var session = _documentStore.QuerySession();
        
        // First, get all phases for the challenge
        var phases = await session.Query<ChallengePhase>()
            .Where(p => p.ChallengeId == challengeId && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync();

        // Then count posts in all those phases
        return await session.Query<ChallengePost>()
            .CountAsync(p => phases.Contains(p.ChallengePhaseId) && !p.IsDeleted);
    }
}
