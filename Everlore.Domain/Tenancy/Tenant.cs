using Everlore.Domain.Common;

namespace Everlore.Domain.Tenancy;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
