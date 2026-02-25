using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record CreateTenantCommand(
    string Name,
    string Identifier,
    string? ConnectionString = null,
    bool IsActive = true,
    HostingMode HostingMode = HostingMode.SaasHosted) : IRequest<Result<TenantDto>>;

public class CreateTenantValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9_-]+$").WithMessage("Identifier must contain only lowercase letters, numbers, hyphens, and underscores.");
        RuleFor(x => x.ConnectionString).MaximumLength(1000);
    }
}

public class CreateTenantHandler(
    ICatalogDbContext db,
    ITenantDatabaseProvisioner provisioner) : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Tenants
            .AnyAsync(t => t.Identifier == request.Identifier, cancellationToken);

        if (exists)
            return Result.Failure<TenantDto>(ResultErrorType.Conflict, $"Tenant with identifier '{request.Identifier}' already exists.");

        string? connectionString = null;
        if (request.HostingMode == HostingMode.SaasHosted)
        {
            connectionString = request.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = await provisioner.ProvisionAsync(request.Identifier, cancellationToken);
            }
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Identifier = request.Identifier,
            ConnectionString = connectionString,
            IsActive = request.IsActive,
            HostingMode = request.HostingMode
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TenantDto(
            tenant.Id, tenant.Name, tenant.Identifier, tenant.IsActive, tenant.CreatedAt, tenant.UpdatedAt));
    }
}
