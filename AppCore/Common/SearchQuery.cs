namespace AppCore.Services;

public class SearchQuery
{
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}
