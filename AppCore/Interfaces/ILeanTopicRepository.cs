using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface ILeanTopicRepository : IRepository<LeanTopic>
{
    Task<List<LeanTopic>> GetBySessionIdAsync(string sessionId);
    Task<List<LeanTopic>> GetBySessionIdAndStatusAsync(string sessionId, TopicStatus status);
    Task<LeanTopic?> GetCurrentTopicAsync(string sessionId);
}
