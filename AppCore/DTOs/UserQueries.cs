namespace AppCore.DTOs
{
    public class GetUserByIdQuery
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class GetUserByEmailQuery
    {
        public string Email { get; set; } = string.Empty;
    }

    public class SearchUsersQuery
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class GetUserActivityHistoryQuery
    {
        public string UserId { get; set; } = string.Empty;
    }
}
