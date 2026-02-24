using Everlore.Application.Common.Interfaces;
using Everlore.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Application.Reporting.Reports;

public record UpdateReportCommand(
    Guid Id,
    string Name,
    string? Description,
    string QueryDefinitionJson,
    string? VisualizationConfigJson,
    bool IsPublic) : IRequest<Result>;

public class UpdateReportValidator : AbstractValidator<UpdateReportCommand>
{
    public UpdateReportValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.QueryDefinitionJson).NotEmpty();
    }
}

public class UpdateReportHandler(
    ICatalogDbContext db,
    ICurrentUser currentUser) : IRequestHandler<UpdateReportCommand, Result>
{
    public async Task<Result> Handle(UpdateReportCommand request, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null)
            return Result.Failure(ResultErrorType.Forbidden, "Tenant context required.");

        var report = await db.ReportDefinitions
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == tenantId.Value, cancellationToken);

        if (report is null)
            return Result.Failure(ResultErrorType.NotFound, $"Report '{request.Id}' not found.");

        report.Name = request.Name;
        report.Description = request.Description;
        report.QueryDefinitionJson = request.QueryDefinitionJson;
        report.VisualizationConfigJson = request.VisualizationConfigJson;
        report.IsPublic = request.IsPublic;
        report.Version++;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
