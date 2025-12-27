using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using Marten;

namespace AppInfra.Repositories;

public class ChallengePostCommentRepository : Repository<ChallengePostComment>, IChallengePostCommentRepository
{
    private readonly IDocumentStore _documentStore;

    public ChallengePostCommentRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<ChallengePostComment>> GetByPostIdAsync(string challengePostId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<ChallengePostComment>()
            .Where(c => c.ChallengePostId == challengePostId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<PagedResults<ChallengePostComment>> GetPagedByPostIdAsync(GetChallengePostCommentsQuery query)
    {
        using var session = _documentStore.QuerySession();
        
        var queryable = session.Query<ChallengePostComment>()
            .Where(c => c.ChallengePostId == query.ChallengePostId && !c.IsDeleted);

        // Get total count
        var totalRecords = await queryable.CountAsync();

        // Apply sorting - oldest first for comments
        queryable = queryable.OrderBy(c => c.CreatedAt);

        // Apply pagination
        var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
        var pageSize = query.PageSize > 0 ? query.PageSize : 20;
        
        queryable = queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await queryable.ToListAsync();
        var itemsList = items.ToList();

        return new PagedResults<ChallengePostComment>
        {
            Data = itemsList,
            TotalCount = totalRecords,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            Success = true,
            Message = "Comments retrieved successfully"
        };
    }

    public async Task<int> GetCommentCountAsync(string challengePostId)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<ChallengePostComment>()
            .CountAsync(c => c.ChallengePostId == challengePostId && !c.IsDeleted);
    }
}
