using Everlore.Domain.Common;

namespace Everlore.Domain.Tenancy;

public class TenantSetting : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
