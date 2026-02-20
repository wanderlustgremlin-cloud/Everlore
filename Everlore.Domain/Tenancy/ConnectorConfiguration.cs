using Everlore.Domain.Common;

namespace Everlore.Domain.Tenancy;

public class ConnectorConfiguration : BaseEntity
{
    public Guid TenantId { get; set; }
    public string ConnectorType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public string? Settings { get; set; }
    public Tenant Tenant { get; set; } = null!;
}
