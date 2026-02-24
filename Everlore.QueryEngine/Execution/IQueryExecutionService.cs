using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Execution;

public interface IQueryExecutionService
{
    Task<QueryResult> ExecuteAsync(QueryDefinition query, DataSource dataSource, CancellationToken ct = default);
}
