namespace Everlore.Gateway.Configuration;

public class GatewaySettings
{
    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public string? TenantDbConnectionString { get; set; }
    public Dictionary<string, DataSourceLocalConfig> DataSources { get; set; } = new();
}

public class DataSourceLocalConfig
{
    public Guid DataSourceId { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public int DataSourceType { get; set; }
}
