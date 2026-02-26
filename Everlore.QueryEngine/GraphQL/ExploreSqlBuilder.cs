using System.Text;
using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Schema;
using HotChocolate.Resolvers;

namespace Everlore.QueryEngine.GraphQL;

public static class ExploreSqlBuilder
{
    public static string BuildExploreSql(IResolverContext context, DataSource dataSource, DiscoveredTable table)
    {
        var selectedFields = context.Selection.SyntaxNode.SelectionSet?.Selections
            .OfType<HotChocolate.Language.FieldNode>()
            .Select(f => f.Name.Value)
            .ToList() ?? [];

        var validColumns = table.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selectColumns = selectedFields.Count > 0
            ? selectedFields.Where(validColumns.Contains).ToList()
            : table.Columns.Select(c => c.Name).ToList();

        if (selectColumns.Count == 0)
            selectColumns = table.Columns.Select(c => c.Name).ToList();

        var quote = GetQuoteFunc(dataSource.Type);

        var sb = new StringBuilder("SELECT ");
        sb.Append(string.Join(", ", selectColumns.Select(quote)));
        sb.Append(" FROM ");
        sb.Append($"{quote(table.SchemaName)}.{quote(table.TableName)}");

        var firstArg = context.ArgumentValue<int?>("first");
        var first = firstArg ?? 100;
        first = Math.Min(first, 1000);
        sb.Append(dataSource.Type == DataSourceType.SqlServer
            ? $" ORDER BY (SELECT NULL) OFFSET 0 ROWS FETCH NEXT {first} ROWS ONLY"
            : $" LIMIT {first}");

        return sb.ToString();
    }

    private static Func<string, string> GetQuoteFunc(DataSourceType type) => type switch
    {
        DataSourceType.PostgreSql => s => $"\"{s}\"",
        DataSourceType.SqlServer => s => $"[{s}]",
        DataSourceType.MySql => s => $"`{s}`",
        _ => s => $"\"{s}\""
    };
}
