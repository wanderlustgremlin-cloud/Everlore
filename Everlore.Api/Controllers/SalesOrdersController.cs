using Everlore.Api.Models;
using Everlore.Domain.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class SalesOrdersController(ISalesOrderRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesOrderResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderResponse>> Create(CreateSalesOrderRequest request, CancellationToken ct)
    {
        var entity = new SalesOrder
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            OrderNumber = request.OrderNumber,
            OrderDate = request.OrderDate,
            Status = request.Status,
            TotalAmount = request.TotalAmount
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateSalesOrderRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.CustomerId = request.CustomerId;
        entity.OrderNumber = request.OrderNumber;
        entity.OrderDate = request.OrderDate;
        entity.Status = request.Status;
        entity.TotalAmount = request.TotalAmount;

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

    private static SalesOrderResponse MapToResponse(SalesOrder entity) => new(
        entity.Id,
        entity.CustomerId,
        entity.OrderNumber,
        entity.OrderDate,
        entity.Status,
        entity.TotalAmount,
        entity.CreatedAt,
        entity.UpdatedAt);
}
