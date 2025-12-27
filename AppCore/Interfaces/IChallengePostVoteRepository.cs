using System.Threading.Tasks;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePostVoteRepository : IRepository<ChallengePostVote>
{
    Task<ChallengePostVote?> GetVoteAsync(string challengePostId, string userId);
    Task<bool> HasUserVotedAsync(string challengePostId, string userId);
    Task<int> GetVoteCountAsync(string challengePostId);
}
