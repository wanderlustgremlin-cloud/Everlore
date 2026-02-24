using Everlore.Domain.Common;

namespace Everlore.Domain.Reporting;

public class DataSource : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string EncryptedConnectionString { get; set; } = string.Empty;
    public DateTime? SchemaLastRefreshedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum DataSourceType
{
    PostgreSql = 0,
    SqlServer = 1,
    MySql = 2
}
