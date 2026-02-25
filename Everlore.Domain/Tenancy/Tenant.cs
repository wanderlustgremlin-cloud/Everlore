using Everlore.Domain.Common;

namespace Everlore.Domain.Tenancy;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public bool IsActive { get; set; } = true;
    public HostingMode HostingMode { get; set; } = HostingMode.SaasHosted;

    public ICollection<ConnectorConfiguration> ConnectorConfigurations { get; set; } = [];
    public ICollection<TenantUser> TenantUsers { get; set; } = [];
}
