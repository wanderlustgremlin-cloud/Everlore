using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record RevokeGatewayApiKeyCommand(Guid TenantId, Guid KeyId) : IRequest<Result>;

public class RevokeGatewayApiKeyHandler(ICatalogDbContext db)
    : IRequestHandler<RevokeGatewayApiKeyCommand, Result>
{
    public async Task<Result> Handle(RevokeGatewayApiKeyCommand request, CancellationToken cancellationToken)
    {
        var key = await db.GatewayApiKeys
            .FirstOrDefaultAsync(k => k.Id == request.KeyId && k.TenantId == request.TenantId, cancellationToken);

        if (key is null)
            return Result.Failure(ResultErrorType.NotFound, "Gateway API key not found.");

        key.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
