using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Tenancy;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Tenancy;

public record UpsertTenantSettingCommand(
    Guid TenantId,
    string Key,
    string Value,
    string? Description = null) : IRequest<Result<TenantSettingDto>>;

public class UpsertTenantSettingValidator : AbstractValidator<UpsertTenantSettingCommand>
{
    public UpsertTenantSettingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpsertTenantSettingHandler(ICatalogDbContext db)
    : IRequestHandler<UpsertTenantSettingCommand, Result<TenantSettingDto>>
{
    public async Task<Result<TenantSettingDto>> Handle(
        UpsertTenantSettingCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await db.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
            return Result.Failure<TenantSettingDto>(ResultErrorType.NotFound, "Tenant not found.");

        var existing = await db.TenantSettings
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && s.Key == request.Key, cancellationToken);

        if (existing is not null)
        {
            existing.Value = request.Value;
            existing.Description = request.Description;
        }
        else
        {
            existing = new TenantSetting
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Key = request.Key,
                Value = request.Value,
                Description = request.Description
            };
            db.TenantSettings.Add(existing);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TenantSettingDto(
            existing.Id, existing.TenantId, existing.Key, existing.Value, existing.Description));
    }
}
