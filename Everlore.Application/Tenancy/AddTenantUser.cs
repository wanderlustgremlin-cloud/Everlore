using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record AddTenantUserCommand(
    Guid TenantId,
    Guid UserId,
    TenantRole Role) : IRequest<Result>;

public class AddTenantUserValidator : AbstractValidator<AddTenantUserCommand>
{
    public AddTenantUserValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class AddTenantUserHandler(ICatalogDbContext db) : IRequestHandler<AddTenantUserCommand, Result>
{
    public async Task<Result> Handle(AddTenantUserCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants
            .AnyAsync(t => t.Id == request.TenantId, cancellationToken);

        if (!tenantExists)
            return Result.Failure(ResultErrorType.NotFound, $"Tenant '{request.TenantId}' not found.");

        var alreadyAssigned = await db.TenantUsers
            .AnyAsync(tu => tu.TenantId == request.TenantId && tu.UserId == request.UserId, cancellationToken);

        if (alreadyAssigned)
            return Result.Failure(ResultErrorType.Conflict, "User is already assigned to this tenant.");

        db.TenantUsers.Add(new TenantUser
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
