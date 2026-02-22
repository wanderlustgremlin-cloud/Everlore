using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Models;
using Everlore.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public abstract class CrudController<T>(IRepository<T> repository) : ApiControllerBase
    where T : BaseEntity
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination, CancellationToken ct)
    {
        var result = await repository.Query().ToPagedResultAsync(pagination, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create(T entity, CancellationToken ct)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        repository.Add(entity);
        await repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, T entity, CancellationToken ct)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is null) return NotFound();

        entity.Id = id;
        repository.SetValues(existing, entity);
        existing.UpdatedAt = DateTime.UtcNow;

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
}
