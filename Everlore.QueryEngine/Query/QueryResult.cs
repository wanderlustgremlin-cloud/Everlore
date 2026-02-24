namespace Everlore.QueryEngine.Query;

public class QueryResult
{
    public List<QueryColumn> Columns { get; set; } = [];
    public List<Dictionary<string, object?>> Rows { get; set; } = [];
    public int RowCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

public class QueryColumn
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
