using Everlore.Api.Gateway;
using Everlore.Application.Common.Models;
using Everlore.Application.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public class TenantsController(ISender sender, IGatewayConnectionTracker gatewayTracker) : ApiControllerBase
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
    // --- Gateway Status ---

    [HttpGet("{tenantId:guid}/gateway-status")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public IActionResult GetGatewayStatus(Guid tenantId)
    {
        var agentInfo = gatewayTracker.GetAgentInfo(tenantId);

        if (agentInfo is null)
        {
            return Ok(new
            {
                IsOnline = false,
                AgentVersion = (string?)null,
                ConnectedAt = (DateTime?)null,
                LastHeartbeatAt = (DateTime?)null,
                AvailableDataSourceIds = Array.Empty<Guid>()
            });
        }

        var heartbeatAge = DateTime.UtcNow - agentInfo.LastHeartbeatAt;
        var isHealthy = heartbeatAge.TotalSeconds < 60; // 2x heartbeat interval

        return Ok(new
        {
            IsOnline = true,
            agentInfo.AgentVersion,
            agentInfo.ConnectedAt,
            agentInfo.LastHeartbeatAt,
            IsHealthy = isHealthy,
            agentInfo.AvailableDataSourceIds
        });
    }

    // --- Gateway API Keys ---

    [HttpPost("{tenantId:guid}/gateway-keys")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GenerateGatewayKey(
        Guid tenantId, GenerateGatewayApiKeyCommand command, CancellationToken ct)
    {
        if (tenantId != command.TenantId) return BadRequest();
        var result = await sender.Send(command, ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpGet("{tenantId:guid}/gateway-keys")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ListGatewayKeys(Guid tenantId, CancellationToken ct)
    {
        var result = await sender.Send(new ListGatewayApiKeysQuery(tenantId), ct);
        return result.IsSuccess ? Ok(result.Value) : ToError(result);
    }

    [HttpDelete("{tenantId:guid}/gateway-keys/{keyId:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> RevokeGatewayKey(Guid tenantId, Guid keyId, CancellationToken ct)
    {
        var result = await sender.Send(new RevokeGatewayApiKeyCommand(tenantId, keyId), ct);
        return result.IsSuccess ? NoContent() : ToError(result);
    }
}

public record UpsertTenantSettingBody(string Value, string? Description = null);
