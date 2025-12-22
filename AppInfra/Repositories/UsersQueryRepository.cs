using System.Linq;
using System.Threading.Tasks;
using AppCore.Entities;
using AppCore.Services;
using Marten;
using Marten.Pagination;

namespace AppInfra.Repositories
{
    public class UsersQueryRepository : IUsersQueryRepository
    {
        private readonly IDocumentStore _documentStore;

        public UsersQueryRepository(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public async Task<PagedResults<User>> GetUsersAsync(SearchQuery query)
        {
            using var session = _documentStore.QuerySession();

            // Build the base query for non-deleted users
            var baseQuery = session.Query<User>()
                .Where(u => !u.IsDeleted);

            // Apply search term filter if provided
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                baseQuery = baseQuery.Where(u =>
                    u.Email.ToLower().Contains(searchTerm) ||
                    u.DisplayName.ToLower().Contains(searchTerm));
            }

            // Get total count before pagination
            var totalCount = await baseQuery.CountAsync();

            // Calculate total pages
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)query.PageSize);

            // Apply pagination using Marten's ToPagedListAsync
            var pagedList = await baseQuery
                .OrderBy(u => u.DisplayName)
                .ToPagedListAsync(query.PageNumber, query.PageSize);

            return new PagedResults<User>
            {
                Success = true,
                Data = pagedList.ToList(),
                TotalCount = (int)totalCount,
                TotalPages = totalPages,
                CurrentPage = query.PageNumber,
                PageSize = query.PageSize,
                Message = $"Retrieved {pagedList.Count} users"
            };
        }
    }
}
