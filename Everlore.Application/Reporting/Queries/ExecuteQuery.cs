using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Queries;

public record ExecuteQueryCommand(
    Guid DataSourceId,
    string Table,
    string? SchemaName,
    object? Measures,
    object? Dimensions,
    object? Filters,
    object? Sorts,
    int? Limit,
    int? Offset) : IRequest<Result<object>>;

public class ExecuteQueryHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    IQueryExecutionService queryExecutionService) : IRequestHandler<ExecuteQueryCommand, Result<object>>
{
    public async Task<Result<object>> Handle(ExecuteQueryCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<object>(ResultErrorType.Forbidden, "Tenant context required.");

        var dataSource = await db.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == request.DataSourceId && ds.TenantId == tenantId.Value, cancellationToken);

        if (dataSource is null)
            return Result.Failure<object>(ResultErrorType.NotFound, $"Data source '{request.DataSourceId}' not found.");

        var result = await queryExecutionService.ExecuteAsync(request, dataSource, cancellationToken);
        return Result.Success(result);
    }
}
