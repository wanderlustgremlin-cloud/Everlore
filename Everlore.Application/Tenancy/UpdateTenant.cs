using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record UpdateTenantCommand(
    Guid Id,
    string Name,
    bool IsActive) : IRequest<Result>;

public class UpdateTenantValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

public class UpdateTenantHandler(ICatalogDbContext db) : IRequestHandler<UpdateTenantCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant is null)
            return Result.Failure(ResultErrorType.NotFound, $"Tenant '{request.Id}' not found.");

        tenant.Name = request.Name;
        tenant.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
