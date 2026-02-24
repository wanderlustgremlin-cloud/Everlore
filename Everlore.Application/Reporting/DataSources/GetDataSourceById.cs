using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record GetDataSourceByIdQuery(Guid Id) : IRequest<Result<DataSourceDto>>;

public class GetDataSourceByIdHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetDataSourceByIdQuery, Result<DataSourceDto>>
{
    public async Task<Result<DataSourceDto>> Handle(GetDataSourceByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;

        var dataSource = await db.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == request.Id && ds.TenantId == tenantId, cancellationToken);

        if (dataSource is null)
            return Result.Failure<DataSourceDto>(ResultErrorType.NotFound, $"Data source '{request.Id}' not found.");

        return Result.Success(DataSourceDto.From(dataSource));
    }
}
