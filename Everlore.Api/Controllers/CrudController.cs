using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Models;
using Everlore.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Everlore.Api.Controllers;

public abstract class CrudController<T>(IRepository<T> repository) : ApiControllerBase
    where T : BaseEntity
{
    private static readonly HashSet<string> PaginationKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "page", "pageSize", "sortBy", "sortDir", "after"
    };

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination,
        [FromQuery] string? after,
        CancellationToken ct)
    {
        var filters = ExtractFilters();
        var query = repository.Query();

        if (!string.IsNullOrWhiteSpace(after))
        {
            var cursorQuery = new CursorPaginationQuery(
                pagination.PageSize, after, pagination.SortBy, pagination.SortDir);
            var cursorResult = await query.ToCursorPagedResultAsync(cursorQuery, filters, ct);
            return Ok(cursorResult);
        }

        var result = await query.ToPagedResultAsync(pagination, filters, ct);
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

    private IDictionary<string, string> ExtractFilters()
    {
        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in HttpContext.Request.Query)
        {
            if (!PaginationKeys.Contains(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
                filters[kvp.Key] = kvp.Value!;
        }
        return filters;
    }
}
