using Everlore.Api.Models;
using Everlore.Domain.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ShipmentsController(IShipmentRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShipmentResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShipmentResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ShipmentResponse>> Create(CreateShipmentRequest request, CancellationToken ct)
    {
        var entity = new Shipment
        {
            Id = Guid.NewGuid(),
            CarrierId = request.CarrierId,
            SalesOrderId = request.SalesOrderId,
            TrackingNumber = request.TrackingNumber,
            Status = request.Status,
            ShippedDate = request.ShippedDate,
            DeliveredDate = request.DeliveredDate,
            ShipToAddress = request.ShipToAddress
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateShipmentRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.CarrierId = request.CarrierId;
        entity.SalesOrderId = request.SalesOrderId;
        entity.TrackingNumber = request.TrackingNumber;
        entity.Status = request.Status;
        entity.ShippedDate = request.ShippedDate;
        entity.DeliveredDate = request.DeliveredDate;
        entity.ShipToAddress = request.ShipToAddress;

        repository.Update(entity);
        await repository.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        repository.Remove(entity);
        await repository.SaveChangesAsync(ct);

        return NoContent();
    }

    private static ShipmentResponse MapToResponse(Shipment entity) => new(
        entity.Id,
        entity.CarrierId,
        entity.SalesOrderId,
        entity.TrackingNumber,
        entity.Status,
        entity.ShippedDate,
        entity.DeliveredDate,
        entity.ShipToAddress,
        entity.CreatedAt,
        entity.UpdatedAt);
}
