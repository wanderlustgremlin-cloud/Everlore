using Everlore.Api.Models;
using Everlore.Domain.AccountsReceivable;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController(IInvoiceRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceResponse>> Create(CreateInvoiceRequest request, CancellationToken ct)
    {
        var entity = new Invoice
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            InvoiceNumber = request.InvoiceNumber,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            TotalAmount = request.TotalAmount,
            AmountPaid = request.AmountPaid,
            Status = request.Status,
            SalesOrderId = request.SalesOrderId
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateInvoiceRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.CustomerId = request.CustomerId;
        entity.InvoiceNumber = request.InvoiceNumber;
        entity.InvoiceDate = request.InvoiceDate;
        entity.DueDate = request.DueDate;
        entity.TotalAmount = request.TotalAmount;
        entity.AmountPaid = request.AmountPaid;
        entity.Status = request.Status;
        entity.SalesOrderId = request.SalesOrderId;

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

    private static InvoiceResponse MapToResponse(Invoice entity) => new(
        entity.Id,
        entity.CustomerId,
        entity.InvoiceNumber,
        entity.InvoiceDate,
        entity.DueDate,
        entity.TotalAmount,
        entity.AmountPaid,
        entity.Status,
        entity.SalesOrderId,
        entity.CreatedAt,
        entity.UpdatedAt);
}
