using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record GatewayApiKeyDto(
    Guid Id,
    string Name,
    string KeyPrefix,
    DateTime? LastUsedAt,
    DateTime? ExpiresAt,
    bool IsRevoked,
    DateTime CreatedAt);

public record ListGatewayApiKeysQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<GatewayApiKeyDto>>>;

public class ListGatewayApiKeysHandler(ICatalogDbContext db)
    : IRequestHandler<ListGatewayApiKeysQuery, Result<IReadOnlyList<GatewayApiKeyDto>>>
{
    public async Task<Result<IReadOnlyList<GatewayApiKeyDto>>> Handle(
        ListGatewayApiKeysQuery request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            return Result.Failure<IReadOnlyList<GatewayApiKeyDto>>(ResultErrorType.NotFound, "Tenant not found.");

        var keys = await db.GatewayApiKeys
            .AsNoTracking()
            .Where(k => k.TenantId == request.TenantId)
            .OrderByDescending(k => k.CreatedAt)
            .Select(k => new GatewayApiKeyDto(
                k.Id, k.Name, k.KeyPrefix, k.LastUsedAt, k.ExpiresAt, k.IsRevoked, k.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<GatewayApiKeyDto>>(keys);
    }
}
