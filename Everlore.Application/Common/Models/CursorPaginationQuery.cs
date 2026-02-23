namespace Everlore.Application.Common.Models;

public record CursorPaginationQuery(
    int PageSize = 25,
    string? After = null,
    string? SortBy = null,
    string? SortDir = "asc")
{
    public int PageSize { get; init; } = PageSize < 1 ? 25 : PageSize > 100 ? 100 : PageSize;
}
