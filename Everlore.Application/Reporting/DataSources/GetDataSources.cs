using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record GetDataSourcesQuery(PaginationQuery Pagination) : IRequest<PagedResult<DataSourceDto>>;

public class GetDataSourcesHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetDataSourcesQuery, PagedResult<DataSourceDto>>
{
    public async Task<PagedResult<DataSourceDto>> Handle(GetDataSourcesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;

        var query = db.DataSources
            .AsNoTracking()
            .Where(ds => ds.TenantId == tenantId)
            .Select(ds => new DataSourceDto(
                ds.Id, ds.TenantId, ds.Name, ds.Type,
                ds.SchemaLastRefreshedAt, ds.IsActive,
                ds.CreatedAt, ds.UpdatedAt));

        return await query.ToPagedResultAsync(request.Pagination, ct: cancellationToken);
    }
}
