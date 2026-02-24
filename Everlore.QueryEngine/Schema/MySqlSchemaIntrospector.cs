using System.Data;
using Dapper;
using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Schema;

public class MySqlSchemaIntrospector : ISchemaIntrospector
{
    public async Task<DiscoveredSchema> IntrospectAsync(IDbConnection connection, Guid dataSourceId, CancellationToken ct = default)
    {
        // Get the current database name
        var database = await connection.ExecuteScalarAsync<string>("SELECT DATABASE()");

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
                AND c.TABLE_SCHEMA = @Database
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION
            """;

        const string pkSql = """
            SELECT
                k.TABLE_SCHEMA AS SchemaName,
                k.TABLE_NAME AS TableName,
                k.COLUMN_NAME AS ColumnName
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE k
            JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                ON k.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                AND k.TABLE_SCHEMA = tc.TABLE_SCHEMA
                AND k.TABLE_NAME = tc.TABLE_NAME
            WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                AND k.TABLE_SCHEMA = @Database
            """;

        var columns = (await connection.QueryAsync<ColumnRow>(columnsSql, new { Database = database })).ToList();
        var pkColumns = (await connection.QueryAsync<PkRow>(pkSql, new { Database = database }))
            .ToHashSet(new PkRowComparer());

        var tables = columns
            .GroupBy(c => (c.SchemaName, c.TableName))
            .Select(g => new DiscoveredTable(
                g.Key.SchemaName,
                g.Key.TableName,
                g.Select(c => new DiscoveredColumn(
                    c.ColumnName,
                    c.DataType,
                    TypeNormalizer.Normalize(c.DataType, DataSourceType.MySql),
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
