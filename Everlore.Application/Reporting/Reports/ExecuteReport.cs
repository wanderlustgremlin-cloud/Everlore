using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record ExecuteReportCommand(Guid ReportId) : IRequest<Result<object>>;

public class ExecuteReportHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    IQueryExecutionService queryExecutionService) : IRequestHandler<ExecuteReportCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ExecuteReportCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<object>(ResultErrorType.Forbidden, "Tenant context required.");

        var report = await db.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.ReportId && r.TenantId == tenantId.Value, cancellationToken);

        if (report is null)
            return Result.Failure<object>(ResultErrorType.NotFound, $"Report '{request.ReportId}' not found.");

        var dataSource = await db.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == report.DataSourceId && ds.TenantId == tenantId.Value, cancellationToken);

        if (dataSource is null)
            return Result.Failure<object>(ResultErrorType.NotFound, $"Data source '{report.DataSourceId}' not found.");

        var result = await queryExecutionService.ExecuteReportAsync(report, dataSource, cancellationToken);
        return Result.Success(result);
    }
}
