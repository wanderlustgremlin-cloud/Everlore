using Everlore.Api.Models;
using Everlore.Domain.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken ct)
    {
        var entities = await repository.GetAllAsync(ct);
        return Ok(entities.Select(MapToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();
        return Ok(MapToResponse(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var entity = new Product
        {
            Id = Guid.NewGuid(),
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            UnitOfMeasure = request.UnitOfMeasure,
            IsActive = request.IsActive
        };

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        var response = MapToResponse(entity);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity is null) return NotFound();

        entity.Sku = request.Sku;
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UnitPrice = request.UnitPrice;
        entity.UnitOfMeasure = request.UnitOfMeasure;
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

    private static ProductResponse MapToResponse(Product entity) => new(
        entity.Id,
        entity.Sku,
        entity.Name,
        entity.Description,
        entity.UnitPrice,
        entity.UnitOfMeasure,
        entity.IsActive,
        entity.CreatedAt,
        entity.UpdatedAt);
}
