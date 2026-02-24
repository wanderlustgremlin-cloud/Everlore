using System.Data;
using Dapper;
using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Schema;

public class PostgresSchemaIntrospector : ISchemaIntrospector
{
    public async Task<DiscoveredSchema> IntrospectAsync(IDbConnection connection, Guid dataSourceId, CancellationToken ct = default)
    {
        const string columnsSql = """
            SELECT
                c.table_schema AS SchemaName,
                c.table_name AS TableName,
                c.column_name AS ColumnName,
                c.data_type AS DataType,
                CASE WHEN c.is_nullable = 'YES' THEN true ELSE false END AS IsNullable
            FROM information_schema.columns c
            JOIN information_schema.tables t
                ON c.table_schema = t.table_schema AND c.table_name = t.table_name
            WHERE t.table_type = 'BASE TABLE'
                AND c.table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY c.table_schema, c.table_name, c.ordinal_position
            """;

        const string pkSql = """
            SELECT
                n.nspname AS SchemaName,
                t.relname AS TableName,
                a.attname AS ColumnName
            FROM pg_constraint con
            JOIN pg_class t ON con.conrelid = t.oid
            JOIN pg_namespace n ON t.relnamespace = n.oid
            JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(con.conkey)
            WHERE con.contype = 'p'
                AND n.nspname NOT IN ('pg_catalog', 'information_schema')
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
                    TypeNormalizer.Normalize(c.DataType, DataSourceType.PostgreSql),
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
