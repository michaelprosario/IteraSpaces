using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface ILeanSessionNoteRepository : IRepository<LeanSessionNote>
{
    Task<List<LeanSessionNote>> GetBySessionIdAsync(string sessionId);
    Task<List<LeanSessionNote>> GetByTopicIdAsync(string topicId);
    Task<List<LeanSessionNote>> GetActionItemsAsync(string sessionId);
}
