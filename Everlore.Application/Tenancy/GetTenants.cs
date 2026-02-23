using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record TenantDto(
    Guid Id,
    string Name,
    string Identifier,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record GetTenantsQuery(PaginationQuery Pagination) : IRequest<PagedResult<TenantDto>>;

public class GetTenantsHandler(ICatalogDbContext db) : IRequestHandler<GetTenantsQuery, PagedResult<TenantDto>>
{
    public async Task<PagedResult<TenantDto>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Tenants
            .AsNoTracking()
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Identifier, t.IsActive, t.CreatedAt, t.UpdatedAt));

        return await query.ToPagedResultAsync(request.Pagination, ct: cancellationToken);
    }
}
