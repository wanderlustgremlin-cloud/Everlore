using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Reporting;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.DataSources;

public record TestDataSourceConnectionCommand(Guid Id) : IRequest<Result<TestConnectionResult>>;

public record TestConnectionResult(bool Success, string Message);

public interface IConnectionTester
{
    Task<TestConnectionResult> TestAsync(DataSource dataSource, CancellationToken ct);
}

public class TestDataSourceConnectionHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser,
    IConnectionTester connectionTester) : IRequestHandler<TestDataSourceConnectionCommand, Result<TestConnectionResult>>
{
    public async Task<Result<TestConnectionResult>> Handle(
        TestDataSourceConnectionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<TestConnectionResult>(ResultErrorType.Forbidden, "Tenant context required.");

        var dataSource = await db.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == request.Id && ds.TenantId == tenantId.Value, cancellationToken);

        if (dataSource is null)
            return Result.Failure<TestConnectionResult>(ResultErrorType.NotFound, $"Data source '{request.Id}' not found.");

        var result = await connectionTester.TestAsync(dataSource, cancellationToken);
        return Result.Success(result);
    }
}
