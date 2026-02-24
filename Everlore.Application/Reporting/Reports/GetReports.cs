using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record GetReportsQuery(PaginationQuery Pagination) : IRequest<PagedResult<ReportDto>>;

public class GetReportsHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetReportsQuery, PagedResult<ReportDto>>
{
    public async Task<PagedResult<ReportDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;

        var query = db.ReportDefinitions
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .Select(r => new ReportDto(
                r.Id, r.TenantId, r.DataSourceId, r.Name, r.Description,
                r.QueryDefinitionJson, r.VisualizationConfigJson,
                r.IsPublic, r.Version, r.CreatedAt, r.UpdatedAt));

        return await query.ToPagedResultAsync(request.Pagination, ct: cancellationToken);
    }
}
