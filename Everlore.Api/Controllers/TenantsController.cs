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

    // --- Tenant Settings ---

    [HttpGet("{tenantId:guid}/settings")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetSettings(Guid tenantId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTenantSettingsQuery(tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpPut("{tenantId:guid}/settings/{key}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> UpsertSetting(
        Guid tenantId, string key, [FromBody] UpsertTenantSettingBody body, CancellationToken ct)
    {
        var result = await sender.Send(
            new UpsertTenantSettingCommand(tenantId, key, body.Value, body.Description), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpDelete("{tenantId:guid}/settings/{key}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeleteSetting(Guid tenantId, string key, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTenantSettingCommand(tenantId, key), ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }
}

public record UpsertTenantSettingBody(string Value, string? Description = null);
