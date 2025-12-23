using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface ILeanTopicVoteRepository : IRepository<LeanTopicVote>
{
    Task<List<LeanTopicVote>> GetByTopicIdAsync(string topicId);
    Task<List<LeanTopicVote>> GetBySessionIdAsync(string sessionId);
    Task<LeanTopicVote?> GetByTopicAndUserIdAsync(string topicId, string userId);
    Task<int> GetVoteCountForTopicAsync(string topicId);
    Task<IEnumerable<LeanTopicVote>> GetBySessionAndUserIdAsync(string sessionId, string userId);
}
