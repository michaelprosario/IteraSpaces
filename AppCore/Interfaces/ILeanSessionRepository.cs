using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Services;
using AppCore.DTOs;
using AppCore.Entities;

namespace AppCore.Interfaces;

public interface ILeanSessionRepository : IRepository<LeanSession>
{
    Task<PagedResults<LeanSession>> SearchAsync(GetLeanSessionsQuery query);
    Task<List<LeanSession>> GetSessionsByFacilitatorAsync(string facilitatorUserId);
    Task<List<LeanSession>> GetSessionsByStatusAsync(SessionStatus status);
}
