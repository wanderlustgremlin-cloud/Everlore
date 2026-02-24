using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Reporting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record CreateDataSourceCommand(
    string Name,
    DataSourceType Type,
    string ConnectionString) : IRequest<Result<DataSourceDto>>;

public class CreateDataSourceValidator : AbstractValidator<CreateDataSourceCommand>
{
    public CreateDataSourceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.ConnectionString).NotEmpty().MaximumLength(2000);
    }
}

public class CreateDataSourceHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    IEncryptionService encryption) : IRequestHandler<CreateDataSourceCommand, Result<DataSourceDto>>
{
    public async Task<Result<DataSourceDto>> Handle(CreateDataSourceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<DataSourceDto>(ResultErrorType.Forbidden, "Tenant context required.");

        var exists = await db.DataSources
            .AnyAsync(ds => ds.TenantId == tenantId.Value && ds.Name == request.Name, cancellationToken);

        if (exists)
            return Result.Failure<DataSourceDto>(ResultErrorType.Conflict, $"Data source '{request.Name}' already exists.");

        var dataSource = new DataSource
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Name = request.Name,
            Type = request.Type,
            EncryptedConnectionString = encryption.Encrypt(request.ConnectionString),
            IsActive = true
        };

        db.DataSources.Add(dataSource);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(DataSourceDto.From(dataSource));
    }
}
