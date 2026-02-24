using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record DeleteDataSourceCommand(Guid Id) : IRequest<Result>;

public class DeleteDataSourceHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<DeleteDataSourceCommand, Result>
{
    public async Task<Result> Handle(DeleteDataSourceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure(ResultErrorType.Forbidden, "Tenant context required.");

        var dataSource = await db.DataSources
            .FirstOrDefaultAsync(ds => ds.Id == request.Id && ds.TenantId == tenantId.Value, cancellationToken);

        if (dataSource is null)
            return Result.Failure(ResultErrorType.NotFound, $"Data source '{request.Id}' not found.");

        db.DataSources.Remove(dataSource);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
