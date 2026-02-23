using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record TenantSettingDto(Guid Id, Guid TenantId, string Key, string Value, string? Description);

public record GetTenantSettingsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<TenantSettingDto>>>;

public class GetTenantSettingsHandler(ICatalogDbContext db)
    : IRequestHandler<GetTenantSettingsQuery, Result<IReadOnlyList<TenantSettingDto>>>
{
    public async Task<Result<IReadOnlyList<TenantSettingDto>>> Handle(
        GetTenantSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
            return Result.Failure<IReadOnlyList<TenantSettingDto>>(ResultErrorType.NotFound, "Tenant not found.");

        var settings = await db.TenantSettings
            .AsNoTracking()
            .Where(s => s.TenantId == request.TenantId)
            .OrderBy(s => s.Key)
            .Select(s => new TenantSettingDto(s.Id, s.TenantId, s.Key, s.Value, s.Description))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TenantSettingDto>>(settings);
    }
}
