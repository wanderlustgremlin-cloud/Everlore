using Everlore.Application.Common.Models;
using Everlore.Application.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public class TenantsController(ISender sender) : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantsQuery(pagination), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantByIdQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(CreateTenantCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : ToError(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, UpdateTenantCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest();
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpGet("{tenantId:guid}/users")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUsers(Guid tenantId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantUsersQuery(tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpPost("{tenantId:guid}/users")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> AddUser(Guid tenantId, AddTenantUserCommand command, CancellationToken ct)
    {
        if (tenantId != command.TenantId) return BadRequest();
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpDelete("{tenantId:guid}/users/{userId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RemoveUser(Guid tenantId, Guid userId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveTenantUserCommand(tenantId, userId), ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }
}
