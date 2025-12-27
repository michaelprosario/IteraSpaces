using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.Interfaces;

public interface IChallengePostCommentRepository : IRepository<ChallengePostComment>
{
    Task<List<ChallengePostComment>> GetByPostIdAsync(string challengePostId);
    Task<PagedResults<ChallengePostComment>> GetPagedByPostIdAsync(GetChallengePostCommentsQuery query);
    Task<int> GetCommentCountAsync(string challengePostId);
}
