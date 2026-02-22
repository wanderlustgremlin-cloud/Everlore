using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record GetTenantByIdQuery(Guid Id) : IRequest<Result<TenantDto>>;

public class GetTenantByIdHandler(ICatalogDbContext db) : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant is null)
            return Result.Failure<TenantDto>(ResultErrorType.NotFound, $"Tenant '{request.Id}' not found.");

        return Result.Success(new TenantDto(
            tenant.Id, tenant.Name, tenant.Identifier, tenant.IsActive, tenant.CreatedAt, tenant.UpdatedAt));
    }
}
