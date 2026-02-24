using Everlore.Domain.Reporting;

namespace Everlore.Application.Reporting.Reports;

public record ReportDto(
    Guid Id,
    Guid TenantId,
    Guid DataSourceId,
    string Name,
    string? Description,
    string QueryDefinitionJson,
    string? VisualizationConfigJson,
    bool IsPublic,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ReportDto From(ReportDefinition r) => new(
        r.Id, r.TenantId, r.DataSourceId, r.Name, r.Description,
        r.QueryDefinitionJson, r.VisualizationConfigJson,
        r.IsPublic, r.Version, r.CreatedAt, r.UpdatedAt);
}
