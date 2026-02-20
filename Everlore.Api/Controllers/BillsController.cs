using Everlore.Api.Models;
using Everlore.Domain.AccountsPayable;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BillsController(IBillRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BillResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<BillResponse>> Create(CreateBillRequest request, CancellationToken ct)
    {
        var entity = new Bill
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            BillNumber = request.BillNumber,
            BillDate = request.BillDate,
            DueDate = request.DueDate,
            TotalAmount = request.TotalAmount,
            AmountPaid = request.AmountPaid,
            Status = request.Status
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBillRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.VendorId = request.VendorId;
        entity.BillNumber = request.BillNumber;
        entity.BillDate = request.BillDate;
        entity.DueDate = request.DueDate;
        entity.TotalAmount = request.TotalAmount;
        entity.AmountPaid = request.AmountPaid;
        entity.Status = request.Status;

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

    private static BillResponse MapToResponse(Bill entity) => new(
        entity.Id,
        entity.VendorId,
        entity.BillNumber,
        entity.BillDate,
        entity.DueDate,
        entity.TotalAmount,
        entity.AmountPaid,
        entity.Status,
        entity.CreatedAt,
        entity.UpdatedAt);
}
