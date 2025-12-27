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

public class ChallengeRepository : Repository<Challenge>, IChallengeRepository
{
    private readonly IDocumentStore _documentStore;

    public ChallengeRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<PagedResults<Challenge>> SearchAsync(GetChallengesQuery query)
    {
        using var session = _documentStore.QuerySession();
        
        var queryable = session.Query<Challenge>()
            .Where(c => !c.IsDeleted);

        // Apply filters
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(c => c.Status == query.Status.Value);
        }

        if (!string.IsNullOrEmpty(query.Category))
        {
            queryable = queryable.Where(c => c.Category == query.Category);
        }

        if (!string.IsNullOrEmpty(query.CreatedByUserId))
        {
            queryable = queryable.Where(c => c.CreatedByUserId == query.CreatedByUserId);
        }

        // Apply search term filter if provided
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            queryable = queryable.Where(c => 
                c.Name.Contains(query.SearchTerm) || 
                c.Description.Contains(query.SearchTerm));
        }

        // Get total count
        var totalRecords = await queryable.CountAsync();

        // Apply sorting - default by CreatedAt descending
        queryable = queryable.OrderByDescending(c => c.CreatedAt);

        // Apply pagination
        var pageNumber = query.PageNumber > 0 ? query.PageNumber : 1;
        var pageSize = query.PageSize > 0 ? query.PageSize : 10;
        
        queryable = queryable
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        var items = await queryable.ToListAsync();
        var itemsList = items.ToList();

        return new PagedResults<Challenge>
        {
            Data = itemsList,
            TotalCount = totalRecords,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
            Success = true,
            Message = "Challenges retrieved successfully"
        };
    }

    public async Task<bool> ChallengeExistsAsync(string name)
    {
        using var session = _documentStore.QuerySession();
        return await session.Query<Challenge>()
            .AnyAsync(c => c.Name == name && !c.IsDeleted);
    }
}
