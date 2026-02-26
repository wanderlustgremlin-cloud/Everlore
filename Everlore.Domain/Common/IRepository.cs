using Everlore.Domain.Common;

namespace Everlore.Domain.Common;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    IQueryable<T> Query();
    Task<int> CountAsync(CancellationToken ct = default);
    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
    void SetValues(T existing, T incoming);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<object> GetAllPagedAsync(int page, int pageSize, string? sortBy, string sortDir,
        string? after, IDictionary<string, string>? filters, CancellationToken ct = default);
}
