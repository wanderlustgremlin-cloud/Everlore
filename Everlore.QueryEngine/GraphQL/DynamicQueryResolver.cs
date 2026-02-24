using System.Data;
using System.Text;
using Dapper;
using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Schema;
using Everlore.QueryEngine.Translation;
using HotChocolate.Resolvers;

namespace Everlore.QueryEngine.GraphQL;

public class DynamicQueryResolver(
    IExternalConnectionFactory connectionFactory,
    SqlTranslatorFactory translatorFactory)
{
    public async Task<IReadOnlyList<Dictionary<string, object?>>> ResolveAsync(
        IResolverContext context,
        DataSource dataSource,
        DiscoveredTable table)
    {
        // Get selected fields from GraphQL query
        var selectedFields = context.Selection.SyntaxNode.SelectionSet?.Selections
            .OfType<HotChocolate.Language.FieldNode>()
            .Select(f => f.Name.Value)
            .ToList() ?? [];

        var validColumns = table.Columns.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Build SELECT with only requested fields
        var selectColumns = selectedFields.Count > 0
            ? selectedFields.Where(validColumns.Contains).ToList()
            : table.Columns.Select(c => c.Name).ToList();

        if (selectColumns.Count == 0)
            selectColumns = table.Columns.Select(c => c.Name).ToList();

        var translator = translatorFactory.Create(dataSource.Type);
        var quote = GetQuoteFunc(dataSource.Type);

        var sb = new StringBuilder("SELECT ");
        sb.Append(string.Join(", ", selectColumns.Select(quote)));
        sb.Append(" FROM ");
        sb.Append($"{quote(table.SchemaName)}.{quote(table.TableName)}");

        // Pagination args
        var firstArg = context.ArgumentValue<int?>("first");
        var first = firstArg ?? 100;
        first = Math.Min(first, 1000);
        sb.Append(dataSource.Type == DataSourceType.SqlServer
            ? $" ORDER BY (SELECT NULL) OFFSET 0 ROWS FETCH NEXT {first} ROWS ONLY"
            : $" LIMIT {first}");

        var sql = sb.ToString();

        using var connection = await connectionFactory.CreateConnectionAsync(dataSource);
        var rows = (await connection.QueryAsync(sql)).ToList();

        return rows.Select(r =>
        {
            var dict = (IDictionary<string, object?>)r;
            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }).ToList();
    }

    private static Func<string, string> GetQuoteFunc(DataSourceType type) => type switch
    {
        DataSourceType.PostgreSql => s => $"\"{s}\"",
        DataSourceType.SqlServer => s => $"[{s}]",
        DataSourceType.MySql => s => $"`{s}`",
        _ => s => $"\"{s}\""
    };
}
