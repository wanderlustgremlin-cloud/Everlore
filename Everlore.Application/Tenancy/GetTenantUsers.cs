using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record TenantUserDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FullName,
    TenantRole Role,
    DateTime CreatedAt);

public record GetTenantUsersQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<TenantUserDto>>>;

public class GetTenantUsersHandler(ICatalogDbContext db) : IRequestHandler<GetTenantUsersQuery, Result<IReadOnlyList<TenantUserDto>>>
{
    public async Task<Result<IReadOnlyList<TenantUserDto>>> Handle(GetTenantUsersQuery request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            return Result.Failure<IReadOnlyList<TenantUserDto>>(ResultErrorType.NotFound, $"Tenant '{request.TenantId}' not found.");

        var users = await db.TenantUsers
            .AsNoTracking()
            .Where(tu => tu.TenantId == request.TenantId)
            .Include(tu => tu.User)
            .Select(tu => new TenantUserDto(
                tu.Id,
                tu.UserId,
                tu.User.Email!,
                tu.User.FullName,
                tu.Role,
                tu.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TenantUserDto>>(users);
    }
}
