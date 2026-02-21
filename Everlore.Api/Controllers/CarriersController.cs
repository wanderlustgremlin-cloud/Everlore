using Everlore.Api.Models;
using Everlore.Domain.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class CarriersController(ICarrierRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CarrierResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CarrierResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<CarrierResponse>> Create(CreateCarrierRequest request, CancellationToken ct)
    {
        var entity = new Carrier
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Code = request.Code,
            ContactEmail = request.ContactEmail,
            Phone = request.Phone,
            IsActive = request.IsActive
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCarrierRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.Name = request.Name;
        entity.Code = request.Code;
        entity.ContactEmail = request.ContactEmail;
        entity.Phone = request.Phone;
        entity.IsActive = request.IsActive;

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

    private static CarrierResponse MapToResponse(Carrier entity) => new(
        entity.Id,
        entity.Name,
        entity.Code,
        entity.ContactEmail,
        entity.Phone,
        entity.IsActive,
        entity.CreatedAt,
        entity.UpdatedAt);
}
