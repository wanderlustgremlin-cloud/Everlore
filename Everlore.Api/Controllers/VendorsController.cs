using Everlore.Api.Models;
using Everlore.Domain.AccountsPayable;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VendorsController(IVendorRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendorResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendorResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<VendorResponse>> Create(CreateVendorRequest request, CancellationToken ct)
    {
        var entity = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            ContactEmail = request.ContactEmail,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = request.IsActive
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateVendorRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.Name = request.Name;
        entity.ContactEmail = request.ContactEmail;
        entity.Phone = request.Phone;
        entity.Address = request.Address;
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

    private static VendorResponse MapToResponse(Vendor entity) => new(
        entity.Id,
        entity.Name,
        entity.ContactEmail,
        entity.Phone,
        entity.Address,
        entity.IsActive,
        entity.CreatedAt,
        entity.UpdatedAt);
}
