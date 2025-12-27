using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface IChallengePhaseRepository : IRepository<ChallengePhase>
{
    Task<List<ChallengePhase>> GetByChallengeIdAsync(string challengeId);
    Task<List<ChallengePhase>> SearchAsync(GetChallengePhasesQuery query);
}
