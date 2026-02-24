namespace Everlore.QueryEngine.Query;

public class QueryDefinition
{
    public Guid DataSourceId { get; set; }
    public string Table { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public List<Measure> Measures { get; set; } = [];
    public List<Dimension> Dimensions { get; set; } = [];
    public List<QueryFilter> Filters { get; set; } = [];
    public List<QuerySort> Sorts { get; set; } = [];
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class Measure
{
    public string Column { get; set; } = string.Empty;
    public AggregateFunction Function { get; set; }
    public string? Alias { get; set; }
}

public enum AggregateFunction
{
    Sum,
    Count,
    Avg,
    Min,
    Max,
    CountDistinct
}

public class Dimension
{
    public string Column { get; set; } = string.Empty;
    public DateBucket? DateBucket { get; set; }
    public string? Alias { get; set; }
}

public enum DateBucket
{
    Day,
    Week,
    Month,
    Quarter,
    Year
}

public class QueryFilter
{
    public string Column { get; set; } = string.Empty;
    public FilterOperator Operator { get; set; }
    public string? Value { get; set; }
    public string? Value2 { get; set; }
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    In,
    Between,
    IsNull,
    IsNotNull
}

public class QuerySort
{
    public string ColumnOrAlias { get; set; } = string.Empty;
    public SortDirection Direction { get; set; } = SortDirection.Asc;
}

public enum SortDirection
{
    Asc,
    Desc
}
