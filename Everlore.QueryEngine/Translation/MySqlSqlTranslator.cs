using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Translation;

public class MySqlSqlTranslator : BaseSqlTranslator
{
    protected override string QuoteIdentifier(string identifier) => $"`{identifier}`";

    protected override string DateBucketExpression(string quotedColumn, DateBucket bucket) => bucket switch
    {
        DateBucket.Day => $"DATE({quotedColumn})",
        DateBucket.Week => $"DATE(DATE_SUB({quotedColumn}, INTERVAL WEEKDAY({quotedColumn}) DAY))",
        DateBucket.Month => $"DATE_FORMAT({quotedColumn}, '%Y-%m-01')",
        DateBucket.Quarter => $"MAKEDATE(YEAR({quotedColumn}), 1) + INTERVAL QUARTER({quotedColumn}) QUARTER - INTERVAL 1 QUARTER",
        DateBucket.Year => $"DATE_FORMAT({quotedColumn}, '%Y-01-01')",
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
