using System.Data;
using Dapper;
using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Schema;

public class SqlServerSchemaIntrospector : ISchemaIntrospector
{
    public async Task<DiscoveredSchema> IntrospectAsync(IDbConnection connection, Guid dataSourceId, CancellationToken ct = default)
    {
        const string columnsSql = """
            SELECT
                c.TABLE_SCHEMA AS SchemaName,
                c.TABLE_NAME AS TableName,
                c.COLUMN_NAME AS ColumnName,
                c.DATA_TYPE AS DataType,
                CASE WHEN c.IS_NULLABLE = 'YES' THEN 1 ELSE 0 END AS IsNullable
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN INFORMATION_SCHEMA.TABLES t
                ON c.TABLE_SCHEMA = t.TABLE_SCHEMA AND c.TABLE_NAME = t.TABLE_NAME
            WHERE t.TABLE_TYPE = 'BASE TABLE'
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
            """;

        const string pkSql = """
            SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName
            FROM sys.indexes i
            JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
            JOIN sys.tables t ON i.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE i.is_primary_key = 1
            """;

        var columns = (await connection.QueryAsync<ColumnRow>(columnsSql)).ToList();
        var pkColumns = (await connection.QueryAsync<PkRow>(pkSql))
            .ToHashSet(new PkRowComparer());

        var tables = columns
            .GroupBy(c => (c.SchemaName, c.TableName))
            .Select(g => new DiscoveredTable(
                g.Key.SchemaName,
                g.Key.TableName,
                g.Select(c => new DiscoveredColumn(
                    c.ColumnName,
                    c.DataType,
                    TypeNormalizer.Normalize(c.DataType, DataSourceType.SqlServer),
                    c.IsNullable,
                    pkColumns.Contains(new PkRow { SchemaName = g.Key.SchemaName, TableName = g.Key.TableName, ColumnName = c.ColumnName })
                )).ToList()))
            .ToList();

        return new DiscoveredSchema(dataSourceId, DateTime.UtcNow, tables);
    }

    private class ColumnRow
    {
        public string SchemaName { get; set; } = "";
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string DataType { get; set; } = "";
        public bool IsNullable { get; set; }
    }

    private class PkRow
    {
        public string SchemaName { get; set; } = "";
        public string TableName { get; set; } = "";
        public string ColumnName { get; set; } = "";
    }

    private class PkRowComparer : IEqualityComparer<PkRow>
    {
        public bool Equals(PkRow? x, PkRow? y) =>
            x?.SchemaName == y?.SchemaName && x?.TableName == y?.TableName && x?.ColumnName == y?.ColumnName;

        public int GetHashCode(PkRow obj) =>
            HashCode.Combine(obj.SchemaName, obj.TableName, obj.ColumnName);
    }
}
