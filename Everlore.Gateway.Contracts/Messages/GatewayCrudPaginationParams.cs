namespace Everlore.Gateway.Contracts.Messages;

public record GatewayCrudPaginationParams(
    int Page,
    int PageSize,
    string? SortBy,
    string SortDir,
    string? After,
    Dictionary<string, string>? Filters);
