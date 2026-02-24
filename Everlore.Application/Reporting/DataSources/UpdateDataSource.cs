using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Reporting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record UpdateDataSourceCommand(
    Guid Id,
    string Name,
    DataSourceType Type,
    string? ConnectionString,
    bool IsActive) : IRequest<Result>;

public class UpdateDataSourceValidator : AbstractValidator<UpdateDataSourceCommand>
{
    public UpdateDataSourceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ConnectionString).MaximumLength(2000);
    }
}

public class UpdateDataSourceHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    IEncryptionService encryption) : IRequestHandler<UpdateDataSourceCommand, Result>
{
    public async Task<Result> Handle(UpdateDataSourceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure(ResultErrorType.Forbidden, "Tenant context required.");

        var dataSource = await db.DataSources
            .FirstOrDefaultAsync(ds => ds.Id == request.Id && ds.TenantId == tenantId.Value, cancellationToken);

        if (dataSource is null)
            return Result.Failure(ResultErrorType.NotFound, $"Data source '{request.Id}' not found.");

        dataSource.Name = request.Name;
        dataSource.Type = request.Type;
        dataSource.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.ConnectionString))
            dataSource.EncryptedConnectionString = encryption.Encrypt(request.ConnectionString);

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
