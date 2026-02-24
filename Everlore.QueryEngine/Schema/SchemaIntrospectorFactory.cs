using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Schema;

public class SchemaIntrospectorFactory
{
    private static readonly PostgresSchemaIntrospector Postgres = new();
    private static readonly SqlServerSchemaIntrospector SqlServer = new();
    private static readonly MySqlSchemaIntrospector MySql = new();

    public ISchemaIntrospector Create(DataSourceType type) => type switch
    {
        DataSourceType.PostgreSql => Postgres,
        DataSourceType.SqlServer => SqlServer,
        DataSourceType.MySql => MySql,
        _ => throw new NotSupportedException($"Unsupported data source type: {type}")
    };
}
