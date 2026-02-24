using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record DeleteReportCommand(Guid Id) : IRequest<Result>;

public class DeleteReportHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<DeleteReportCommand, Result>
{
    public async Task<Result> Handle(DeleteReportCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure(ResultErrorType.Forbidden, "Tenant context required.");

        var report = await db.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId.Value, cancellationToken);

        if (report is null)
            return Result.Failure(ResultErrorType.NotFound, $"Report '{request.Id}' not found.");

        db.ReportDefinitions.Remove(report);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
