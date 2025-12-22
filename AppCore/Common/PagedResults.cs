using AppCore.Common;

namespace AppCore.Services;

public class PagedResults<T> : AppResult <List<T>>
{
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
