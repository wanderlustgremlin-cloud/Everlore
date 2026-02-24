using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Translation;

public class PostgresSqlTranslator : BaseSqlTranslator
{
    protected override string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    protected override string DateBucketExpression(string quotedColumn, DateBucket bucket) => bucket switch
    {
        DateBucket.Day => $"DATE_TRUNC('day', {quotedColumn})",
        DateBucket.Week => $"DATE_TRUNC('week', {quotedColumn})",
        DateBucket.Month => $"DATE_TRUNC('month', {quotedColumn})",
        DateBucket.Quarter => $"DATE_TRUNC('quarter', {quotedColumn})",
        DateBucket.Year => $"DATE_TRUNC('year', {quotedColumn})",
        _ => throw new NotSupportedException($"Unsupported date bucket: {bucket}")
    };

    protected override string LimitOffsetClause(int? limit, int? offset)
    {
        var clause = "";
        if (limit.HasValue) clause += $" LIMIT {limit.Value}";
        if (offset.HasValue) clause += $" OFFSET {offset.Value}";
        return clause;
    }
}
