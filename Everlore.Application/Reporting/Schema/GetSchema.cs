using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Schema;

public record GetSchemaQuery(Guid DataSourceId) : IRequest<Result<object>>;

public class GetSchemaHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    ISchemaService schemaService) : IRequestHandler<GetSchemaQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetSchemaQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<object>(ResultErrorType.Forbidden, "Tenant context required.");

        var exists = await db.DataSources
            .AnyAsync(ds => ds.Id == request.DataSourceId && ds.TenantId == tenantId.Value, cancellationToken);

        if (!exists)
            return Result.Failure<object>(ResultErrorType.NotFound, $"Data source '{request.DataSourceId}' not found.");

        var schema = await schemaService.GetSchemaAsync(request.DataSourceId, forceRefresh: false, cancellationToken);
        return Result.Success(schema);
    }
}
