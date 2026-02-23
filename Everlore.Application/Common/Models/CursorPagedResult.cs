namespace Everlore.Application.Common.Models;

public record CursorPagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage,
    string? StartCursor,
    string? EndCursor);
