using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record GetReportByIdQuery(Guid Id) : IRequest<Result<ReportDto>>;

public class GetReportByIdHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetReportByIdQuery, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;

        var report = await db.ReportDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId, cancellationToken);

        if (report is null)
            return Result.Failure<ReportDto>(ResultErrorType.NotFound, $"Report '{request.Id}' not found.");

        return Result.Success(ReportDto.From(report));
    }
}
