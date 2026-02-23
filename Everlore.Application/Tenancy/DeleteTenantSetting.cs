using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record DeleteTenantSettingCommand(Guid TenantId, string Key) : IRequest<Result>;

public class DeleteTenantSettingHandler(ICatalogDbContext db)
    : IRequestHandler<DeleteTenantSettingCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantSettingCommand request, CancellationToken cancellationToken)
    {
        var setting = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && s.Key == request.Key, cancellationToken);

        if (setting is null)
            return Result.Failure(ResultErrorType.NotFound, $"Setting '{request.Key}' not found for tenant.");

        db.TenantSettings.Remove(setting);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
