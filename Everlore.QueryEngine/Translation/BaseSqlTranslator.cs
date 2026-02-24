using System.Text;
using Dapper;
using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Translation;

public abstract class BaseSqlTranslator : ISqlTranslator
{
    public (string Sql, DynamicParameters Parameters) Translate(QueryDefinition query, HashSet<string> validColumns)
    {
        var parameters = new DynamicParameters();
        var sb = new StringBuilder();
        var paramIndex = 0;

        // SELECT
        sb.Append("SELECT ");
        var selectParts = new List<string>();

        foreach (var dim in query.Dimensions)
        {
            ValidateColumn(dim.Column, validColumns);
            var colExpr = dim.DateBucket.HasValue
                ? DateBucketExpression(QuoteIdentifier(dim.Column), dim.DateBucket.Value)
                : QuoteIdentifier(dim.Column);
            var alias = dim.Alias ?? dim.Column;
            selectParts.Add($"{colExpr} AS {QuoteIdentifier(alias)}");
        }

        foreach (var measure in query.Measures)
        {
            ValidateColumn(measure.Column, validColumns);
            var agg = AggregateExpression(QuoteIdentifier(measure.Column), measure.Function);
            var alias = measure.Alias ?? $"{measure.Function}_{measure.Column}";
            selectParts.Add($"{agg} AS {QuoteIdentifier(alias)}");
        }

        if (selectParts.Count == 0)
            selectParts.Add("*");

        sb.Append(string.Join(", ", selectParts));

        // FROM
        sb.Append(" FROM ");
        sb.Append(QuoteTableName(query.SchemaName, query.Table));

        // WHERE
        if (query.Filters.Count > 0)
        {
            sb.Append(" WHERE ");
            var filterParts = new List<string>();

            foreach (var filter in query.Filters)
            {
                ValidateColumn(filter.Column, validColumns);
                var (filterSql, newIndex) = BuildFilter(filter, parameters, paramIndex);
                filterParts.Add(filterSql);
                paramIndex = newIndex;
            }

            sb.Append(string.Join(" AND ", filterParts));
        }

        // GROUP BY
        if (query.Dimensions.Count > 0 && query.Measures.Count > 0)
        {
            sb.Append(" GROUP BY ");
            var groupParts = query.Dimensions.Select(dim =>
                dim.DateBucket.HasValue
                    ? DateBucketExpression(QuoteIdentifier(dim.Column), dim.DateBucket.Value)
                    : QuoteIdentifier(dim.Column));
            sb.Append(string.Join(", ", groupParts));
        }

        // ORDER BY
        if (query.Sorts.Count > 0)
        {
            sb.Append(" ORDER BY ");
            var sortParts = query.Sorts.Select(s =>
            {
                var col = QuoteIdentifier(s.ColumnOrAlias);
                return s.Direction == SortDirection.Desc ? $"{col} DESC" : $"{col} ASC";
            });
            sb.Append(string.Join(", ", sortParts));
        }

        // LIMIT/OFFSET
        if (query.Limit.HasValue || query.Offset.HasValue)
        {
            sb.Append(LimitOffsetClause(query.Limit, query.Offset));
        }

        return (sb.ToString(), parameters);
    }

    protected abstract string QuoteIdentifier(string identifier);
    protected abstract string DateBucketExpression(string quotedColumn, DateBucket bucket);
    protected abstract string LimitOffsetClause(int? limit, int? offset);

    protected virtual string QuoteTableName(string? schema, string table)
    {
        return schema is not null
            ? $"{QuoteIdentifier(schema)}.{QuoteIdentifier(table)}"
            : QuoteIdentifier(table);
    }

    private static string AggregateExpression(string quotedColumn, AggregateFunction function) => function switch
    {
        AggregateFunction.Sum => $"SUM({quotedColumn})",
        AggregateFunction.Count => $"COUNT({quotedColumn})",
        AggregateFunction.Avg => $"AVG({quotedColumn})",
        AggregateFunction.Min => $"MIN({quotedColumn})",
        AggregateFunction.Max => $"MAX({quotedColumn})",
        AggregateFunction.CountDistinct => $"COUNT(DISTINCT {quotedColumn})",
        _ => throw new NotSupportedException($"Unsupported aggregate function: {function}")
    };

    private (string Sql, int NextIndex) BuildFilter(QueryFilter filter, DynamicParameters parameters, int paramIndex)
    {
        var quotedCol = QuoteIdentifier(filter.Column);
        var paramName = $"@p{paramIndex}";

        switch (filter.Operator)
        {
            case FilterOperator.IsNull:
                return ($"{quotedCol} IS NULL", paramIndex);
            case FilterOperator.IsNotNull:
                return ($"{quotedCol} IS NOT NULL", paramIndex);
            case FilterOperator.Equals:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} = {paramName}", paramIndex + 1);
            case FilterOperator.NotEquals:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} <> {paramName}", paramIndex + 1);
            case FilterOperator.GreaterThan:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} > {paramName}", paramIndex + 1);
            case FilterOperator.GreaterThanOrEqual:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} >= {paramName}", paramIndex + 1);
            case FilterOperator.LessThan:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} < {paramName}", paramIndex + 1);
            case FilterOperator.LessThanOrEqual:
                parameters.Add(paramName, filter.Value);
                return ($"{quotedCol} <= {paramName}", paramIndex + 1);
            case FilterOperator.Contains:
                parameters.Add(paramName, $"%{filter.Value}%");
                return ($"{quotedCol} LIKE {paramName}", paramIndex + 1);
            case FilterOperator.StartsWith:
                parameters.Add(paramName, $"{filter.Value}%");
                return ($"{quotedCol} LIKE {paramName}", paramIndex + 1);
            case FilterOperator.EndsWith:
                parameters.Add(paramName, $"%{filter.Value}");
                return ($"{quotedCol} LIKE {paramName}", paramIndex + 1);
            case FilterOperator.In:
                var values = filter.Value?.Split(',').Select(v => v.Trim()).ToArray() ?? [];
                parameters.Add(paramName, values);
                return ($"{quotedCol} IN {paramName}", paramIndex + 1);
            case FilterOperator.Between:
                var paramName2 = $"@p{paramIndex + 1}";
                parameters.Add(paramName, filter.Value);
                parameters.Add(paramName2, filter.Value2);
                return ($"{quotedCol} BETWEEN {paramName} AND {paramName2}", paramIndex + 2);
            default:
                throw new NotSupportedException($"Unsupported filter operator: {filter.Operator}");
        }
    }

    private static void ValidateColumn(string column, HashSet<string> validColumns)
    {
        if (!validColumns.Contains(column))
            throw new InvalidOperationException($"Column '{column}' is not present in the data source schema.");
    }
}
