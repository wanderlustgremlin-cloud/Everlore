using Everlore.Application.Reporting.Queries;
using Everlore.Domain.Reporting;

namespace Everlore.Application.Common.Interfaces;

public interface IQueryExecutionService
{
    Task<object> ExecuteAsync(ExecuteQueryCommand query, DataSource dataSource, CancellationToken ct = default);
    Task<object> ExecuteReportAsync(ReportDefinition report, DataSource dataSource, CancellationToken ct = default);
}
