namespace Everlore.QueryEngine.Schema;

public record DiscoveredSchema(
    Guid DataSourceId,
    DateTime DiscoveredAt,
    IReadOnlyList<DiscoveredTable> Tables);

public record DiscoveredTable(
    string SchemaName,
    string TableName,
    IReadOnlyList<DiscoveredColumn> Columns);

public record DiscoveredColumn(
    string Name,
    string DataType,
    NormalizedType NormalizedType,
    bool IsNullable,
    bool IsPrimaryKey);
