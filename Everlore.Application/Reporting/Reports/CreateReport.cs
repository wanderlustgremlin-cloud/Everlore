using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using Everlore.Domain.Reporting;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record CreateReportCommand(
    Guid DataSourceId,
    string Name,
    string? Description,
    string QueryDefinitionJson,
    string? VisualizationConfigJson,
    bool IsPublic) : IRequest<Result<ReportDto>>;

public class CreateReportValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.DataSourceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.QueryDefinitionJson).NotEmpty();
    }
}

public class CreateReportHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<CreateReportCommand, Result<ReportDto>>
{
    public async Task<Result<ReportDto>> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure<ReportDto>(ResultErrorType.Forbidden, "Tenant context required.");

        var dataSourceExists = await db.DataSources
            .AnyAsync(ds => ds.Id == request.DataSourceId && ds.TenantId == tenantId.Value, cancellationToken);

        if (!dataSourceExists)
            return Result.Failure<ReportDto>(ResultErrorType.NotFound, $"Data source '{request.DataSourceId}' not found.");

        var report = new ReportDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            DataSourceId = request.DataSourceId,
            Name = request.Name,
            Description = request.Description,
            QueryDefinitionJson = request.QueryDefinitionJson,
            VisualizationConfigJson = request.VisualizationConfigJson,
            IsPublic = request.IsPublic,
            Version = 1
        };

        db.ReportDefinitions.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(ReportDto.From(report));
    }
}
