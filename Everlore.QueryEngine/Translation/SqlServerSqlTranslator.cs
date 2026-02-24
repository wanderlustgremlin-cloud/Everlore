using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Translation;

public class SqlServerSqlTranslator : BaseSqlTranslator
{
    protected override string QuoteIdentifier(string identifier) => $"[{identifier}]";

    protected override string DateBucketExpression(string quotedColumn, DateBucket bucket) => bucket switch
    {
        DateBucket.Day => $"CAST(CAST({quotedColumn} AS DATE) AS DATETIME)",
        DateBucket.Week => $"DATEADD(WEEK, DATEDIFF(WEEK, 0, {quotedColumn}), 0)",
        DateBucket.Month => $"DATEFROMPARTS(YEAR({quotedColumn}), MONTH({quotedColumn}), 1)",
        DateBucket.Quarter => $"DATEFROMPARTS(YEAR({quotedColumn}), (DATEPART(QUARTER, {quotedColumn}) - 1) * 3 + 1, 1)",
        DateBucket.Year => $"DATEFROMPARTS(YEAR({quotedColumn}), 1, 1)",
        _ => throw new NotSupportedException($"Unsupported date bucket: {bucket}")
    };

    protected override string LimitOffsetClause(int? limit, int? offset)
    {
        // SQL Server requires ORDER BY for OFFSET/FETCH
        var off = offset ?? 0;
        var clause = $" OFFSET {off} ROWS";
        if (limit.HasValue) clause += $" FETCH NEXT {limit.Value} ROWS ONLY";
        return clause;
    }
}
