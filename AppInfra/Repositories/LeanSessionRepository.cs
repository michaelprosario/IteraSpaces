using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Services;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class LeanSessionRepository : Repository<LeanSession>, ILeanSessionRepository
{
    private readonly IDocumentStore _documentStore;

    public LeanSessionRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<PagedResults<LeanSession>> SearchAsync(GetLeanSessionsQuery query)
    {
        using var session = _documentStore.QuerySession();
        
        var queryable = session.Query<LeanSession>()
            .Where(s => !s.IsDeleted);

        // Apply filters
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(s => s.Status == query.Status.Value);
        }

        if (!string.IsNullOrEmpty(query.FacilitatorUserId))
        {
            queryable = queryable.Where(s => s.FacilitatorUserId == query.FacilitatorUserId);
        }

        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            queryable = queryable.Where(s => 
                s.Title.Contains(query.SearchTerm) || 
                s.Description.Contains(query.SearchTerm));
        }

        // Get total count
        var totalRecords = await queryable.CountAsync();

        // Apply pagination
        var sessionsResult = await queryable
            .OrderByDescending(s => s.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
        var sessions = sessionsResult.ToList();

        return new PagedResults<LeanSession>
        {
            Data = sessions,
            TotalCount = totalRecords,
            CurrentPage = query.PageNumber,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalRecords / query.PageSize),
            Success = true,
            Message = "Sessions retrieved successfully"
        };
    }

    public async Task<List<LeanSession>> GetSessionsByFacilitatorAsync(string facilitatorUserId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanSession>()
            .Where(s => s.FacilitatorUserId == facilitatorUserId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanSession>> GetSessionsByStatusAsync(SessionStatus status)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanSession>()
            .Where(s => s.Status == status && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }
}
