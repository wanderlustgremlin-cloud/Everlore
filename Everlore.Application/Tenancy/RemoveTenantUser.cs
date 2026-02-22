using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record RemoveTenantUserCommand(
    Guid TenantId,
    Guid UserId) : IRequest<Result>;

public class RemoveTenantUserValidator : AbstractValidator<RemoveTenantUserCommand>
{
    public RemoveTenantUserValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public class RemoveTenantUserHandler(ICatalogDbContext db) : IRequestHandler<RemoveTenantUserCommand, Result>
{
    public async Task<Result> Handle(RemoveTenantUserCommand request, CancellationToken cancellationToken)
    {
        var tenantUser = await db.TenantUsers
            .FirstOrDefaultAsync(tu => tu.TenantId == request.TenantId && tu.UserId == request.UserId, cancellationToken);

        if (tenantUser is null)
            return Result.Failure(ResultErrorType.NotFound, "User is not assigned to this tenant.");

        db.TenantUsers.Remove(tenantUser);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
