using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Interfaces;
using Marten;

namespace AppInfra.Repositories;

public class LeanSessionNoteRepository : Repository<LeanSessionNote>, ILeanSessionNoteRepository
{
    private readonly IDocumentStore _documentStore;

    public LeanSessionNoteRepository(IDocumentStore documentStore) : base(documentStore)
    {
        _documentStore = documentStore;
    }

    public async Task<List<LeanSessionNote>> GetBySessionIdAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanSessionNote>()
            .Where(n => n.LeanSessionId == sessionId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanSessionNote>> GetByTopicIdAsync(string topicId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanSessionNote>()
            .Where(n => n.LeanTopicId == topicId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }

    public async Task<List<LeanSessionNote>> GetActionItemsAsync(string sessionId)
    {
        using var session = _documentStore.QuerySession();
        var result = await session.Query<LeanSessionNote>()
            .Where(n => 
                n.LeanSessionId == sessionId && 
                n.NoteType == NoteType.ActionItem && 
                !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        return result.ToList();
    }
}
