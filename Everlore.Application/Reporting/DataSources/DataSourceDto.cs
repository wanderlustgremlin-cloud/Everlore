using Everlore.Domain.Reporting;

namespace Everlore.Application.Reporting.DataSources;

public record DataSourceDto(
    Guid Id,
    Guid TenantId,
    string Name,
    DataSourceType Type,
    DateTime? SchemaLastRefreshedAt,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static DataSourceDto From(DataSource ds) => new(
        ds.Id, ds.TenantId, ds.Name, ds.Type,
        ds.SchemaLastRefreshedAt, ds.IsActive,
        ds.CreatedAt, ds.UpdatedAt);
}
