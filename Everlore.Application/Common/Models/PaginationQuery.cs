namespace Everlore.Application.Common.Models;

public record PaginationQuery(
    int Page = 1,
    int PageSize = 25,
    string? SortBy = null,
    string SortDir = "asc")
{
    public int Page { get; init; } = Page < 1 ? 1 : Page;
    public int PageSize { get; init; } = PageSize < 1 ? 25 : PageSize > 100 ? 100 : PageSize;
}
