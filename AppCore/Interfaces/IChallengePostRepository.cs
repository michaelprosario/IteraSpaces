using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.Interfaces;

public interface IChallengePostRepository : IRepository<ChallengePost>
{
    Task<PagedResults<ChallengePost>> SearchAsync(GetChallengePostsQuery query);
    Task<int> GetPostCountByPhaseIdAsync(string challengePhaseId);
    Task<int> GetPostCountByChallengeIdAsync(string challengeId);
}
