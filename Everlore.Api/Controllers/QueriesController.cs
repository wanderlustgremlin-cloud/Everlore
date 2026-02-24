using Everlore.Application.Reporting.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public class QueriesController(ISender sender) : ApiControllerBase
{
    [HttpPost("execute")]
    public async Task<IActionResult> Execute(ExecuteQueryCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }
}
