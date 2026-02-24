using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Translation;

public class SqlTranslatorFactory
{
    private static readonly PostgresSqlTranslator Postgres = new();
    private static readonly SqlServerSqlTranslator SqlServer = new();
    private static readonly MySqlSqlTranslator MySql = new();

    public ISqlTranslator Create(DataSourceType type) => type switch
    {
        DataSourceType.PostgreSql => Postgres,
        DataSourceType.SqlServer => SqlServer,
        DataSourceType.MySql => MySql,
        _ => throw new NotSupportedException($"Unsupported data source type: {type}")
    };
}
