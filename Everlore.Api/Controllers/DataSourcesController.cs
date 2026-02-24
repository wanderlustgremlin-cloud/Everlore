using Everlore.Application.Common.Models;
using Everlore.Application.Reporting.DataSources;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public class DataSourcesController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination, CancellationToken ct)
    {
        var result = await sender.Send(new GetDataSourcesQuery(pagination), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDataSourceByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDataSourceCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToError(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDataSourceCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteDataSourceCommand(id), ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> TestConnection(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new TestDataSourceConnectionCommand(id), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }
}
