using Everlore.Domain.Common;

namespace Everlore.Domain.Reporting;

public class ReportDefinition : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid DataSourceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string QueryDefinitionJson { get; set; } = string.Empty;
    public string? VisualizationConfigJson { get; set; }
    public bool IsPublic { get; set; }
    public int Version { get; set; } = 1;
}
