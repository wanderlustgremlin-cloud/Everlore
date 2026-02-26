using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Models;
using Everlore.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Infrastructure.Persistence;

public class Repository<T>(EverloreDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly EverloreDbContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DbSet.FindAsync([id], ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await DbSet.ToListAsync(ct);

    public IQueryable<T> Query() => DbSet.AsQueryable();

    public Task<int> CountAsync(CancellationToken ct = default)
        => DbSet.CountAsync(ct);

    public void Add(T entity) => DbSet.Add(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    public void SetValues(T existing, T incoming)
    {
        var createdAt = existing.CreatedAt;
        var createdBy = existing.CreatedBy;
        Context.Entry(existing).CurrentValues.SetValues(incoming);
        existing.CreatedAt = createdAt;
        existing.CreatedBy = createdBy;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);

    public async Task<object> GetAllPagedAsync(int page, int pageSize, string? sortBy, string sortDir,
        string? after, IDictionary<string, string>? filters, CancellationToken ct = default)
    {
        var query = Query();

        if (!string.IsNullOrWhiteSpace(after))
        {
            var cursorQuery = new CursorPaginationQuery(pageSize, after, sortBy, sortDir);
            return await query.ToCursorPagedResultAsync(cursorQuery, filters, ct);
        }

        var pagination = new PaginationQuery(page, pageSize, sortBy, sortDir);
        return await query.ToPagedResultAsync(pagination, filters, ct);
    }
}
