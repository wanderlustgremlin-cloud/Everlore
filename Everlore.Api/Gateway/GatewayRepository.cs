using System.Text.Json;
using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Common;
using Everlore.Domain.Tenancy;
using Everlore.Gateway.Contracts.Messages;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Api.Gateway;

public class GatewayRepository<T>(
    IRepository<T> inner,
    ICatalogDbContext catalogDb,
    ICurrentUser currentUser,
    GatewayCrudService crudService,
    ILogger<GatewayRepository<T>> logger) : IRepository<T> where T : BaseEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<GatewayRepository<T>> _logger = logger;
    private readonly List<(CrudOperation Op, T Entity)> _pending = [];

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(ct))
            return await inner.GetByIdAsync(id, ct);

        var tenantId = currentUser.TenantId!.Value;
        var requestId = Guid.NewGuid().ToString("N");
        var request = new GatewayCrudRequest(requestId, typeof(T).Name, CrudOperation.GetById, id, null, null, null);

        var response = await crudService.SendAsync(tenantId, request, ct);

        if (!response.Success)
        {
            if (response.StatusCode == 404) return null;
            throw new InvalidOperationException($"Gateway CRUD GetById failed: {response.Error}");
        }

        return JsonSerializer.Deserialize<T>(response.ResultJson!, JsonOptions);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(ct))
            return await inner.GetAllAsync(ct);

        // For self-hosted, use GetAllPaged with large page size
        var result = await GetAllPagedAsync(1, 100, null, "asc", null, null, ct);
        // Extract items from the paged result
        var json = JsonSerializer.Serialize(result, JsonOptions);
        using var doc = JsonDocument.Parse(json);
        var itemsJson = doc.RootElement.GetProperty("items").GetRawText();
        return JsonSerializer.Deserialize<List<T>>(itemsJson, JsonOptions) ?? [];
    }

    public IQueryable<T> Query()
    {
        // IQueryable can't be proxied over SignalR — this is only used for SaasHosted
        return inner.Query();
    }

    public Task<int> CountAsync(CancellationToken ct = default) => inner.CountAsync(ct);

    public void Add(T entity) => _pending.Add((CrudOperation.Create, entity));

    public void Update(T entity) => _pending.Add((CrudOperation.Update, entity));

    public void Remove(T entity) => _pending.Add((CrudOperation.Delete, entity));

    public void SetValues(T existing, T incoming) => inner.SetValues(existing, incoming);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(ct) || _pending.Count == 0)
            return await inner.SaveChangesAsync(ct);

        var tenantId = currentUser.TenantId!.Value;
        var count = 0;

        foreach (var (op, entity) in _pending)
        {
            var requestId = Guid.NewGuid().ToString("N");
            var entityJson = JsonSerializer.Serialize(entity, entity.GetType(), JsonOptions);
            var request = new GatewayCrudRequest(requestId, typeof(T).Name, op, entity.Id, entityJson, null, null);

            var response = await crudService.SendAsync(tenantId, request, ct);

            if (!response.Success)
                throw new InvalidOperationException($"Gateway CRUD {op} failed: {response.Error}");

            count++;
        }

        _pending.Clear();
        return count;
    }

    public async Task<object> GetAllPagedAsync(int page, int pageSize, string? sortBy, string sortDir,
        string? after, IDictionary<string, string>? filters, CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(ct))
            return await inner.GetAllPagedAsync(page, pageSize, sortBy, sortDir, after, filters, ct);

        var tenantId = currentUser.TenantId!.Value;
        var requestId = Guid.NewGuid().ToString("N");

        var paginationParams = new GatewayCrudPaginationParams(
            page, pageSize, sortBy, sortDir, after,
            filters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        var paginationJson = JsonSerializer.Serialize(paginationParams, JsonOptions);
        var request = new GatewayCrudRequest(requestId, typeof(T).Name, CrudOperation.GetAll, null, null, paginationJson, after);

        var response = await crudService.SendAsync(tenantId, request, ct);

        if (!response.Success)
            throw new InvalidOperationException($"Gateway CRUD GetAll failed: {response.Error}");

        return JsonSerializer.Deserialize<object>(response.ResultJson!, JsonOptions)!;
    }

    private async Task<bool> IsSelfHostedAsync(CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null) return false;

        var tenant = await catalogDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value, ct);

        return tenant?.HostingMode == HostingMode.SelfHosted;
    }
}
