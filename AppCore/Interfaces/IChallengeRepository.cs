using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.Interfaces;

public interface IChallengeRepository : IRepository<Challenge>
{
    Task<PagedResults<Challenge>> SearchAsync(GetChallengesQuery query);
    Task<bool> ChallengeExistsAsync(string name);
}
