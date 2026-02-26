using System.Text.Json;
using Everlore.Application.Common.Extensions;
using Everlore.Application.Common.Models;
using Everlore.Domain.AccountsPayable;
using Everlore.Domain.AccountsReceivable;
using Everlore.Domain.Common;
using Everlore.Domain.Inventory;
using Everlore.Domain.Sales;
using Everlore.Domain.Shipping;
using Everlore.Gateway.Contracts.Messages;
using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Everlore.Gateway.Handlers;

public class CrudHandler(
    IServiceProvider serviceProvider,
    ILogger<CrudHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, Type> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Vendor"] = typeof(Vendor),
        ["Bill"] = typeof(Bill),
        ["Customer"] = typeof(Customer),
        ["Invoice"] = typeof(Invoice),
        ["Product"] = typeof(Product),
        ["Warehouse"] = typeof(Warehouse),
        ["SalesOrder"] = typeof(SalesOrder),
        ["Carrier"] = typeof(Carrier),
        ["Shipment"] = typeof(Shipment)
    };

    public async Task<GatewayCrudResponse> HandleAsync(GatewayCrudRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Handling CRUD {Operation} for {EntityType} (request {RequestId})",
                request.Operation, request.EntityType, request.RequestId);

            if (!EntityTypeMap.TryGetValue(request.EntityType, out var entityType))
                return new GatewayCrudResponse(request.RequestId, false, null, 400, $"Unknown entity type: {request.EntityType}");

            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EverloreDbContext>();

            return request.Operation switch
            {
                CrudOperation.GetAll => await HandleGetAll(dbContext, entityType, request, ct),
                CrudOperation.GetById => await HandleGetById(dbContext, entityType, request, ct),
                CrudOperation.Create => await HandleCreate(dbContext, entityType, request, ct),
                CrudOperation.Update => await HandleUpdate(dbContext, entityType, request, ct),
                CrudOperation.Delete => await HandleDelete(dbContext, entityType, request, ct),
                _ => new GatewayCrudResponse(request.RequestId, false, null, 400, $"Unknown operation: {request.Operation}")
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CRUD {Operation} for {EntityType} (request {RequestId}) failed",
                request.Operation, request.EntityType, request.RequestId);
            return new GatewayCrudResponse(request.RequestId, false, null, 500, ex.Message);
        }
    }

    private async Task<GatewayCrudResponse> HandleGetAll(
        EverloreDbContext dbContext, Type entityType, GatewayCrudRequest request, CancellationToken ct)
    {
        // Use reflection to call the generic method
        var method = typeof(CrudHandler).GetMethod(nameof(GetAllTyped),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(entityType);
        var result = await (Task<object>)generic.Invoke(null, [dbContext, request, ct])!;

        var json = JsonSerializer.Serialize(result, JsonOptions);
        return new GatewayCrudResponse(request.RequestId, true, json, 200, null);
    }

    private static async Task<object> GetAllTyped<T>(
        EverloreDbContext dbContext, GatewayCrudRequest request, CancellationToken ct) where T : BaseEntity
    {
        var query = dbContext.Set<T>().AsQueryable();

        GatewayCrudPaginationParams? pagination = null;
        if (!string.IsNullOrWhiteSpace(request.PaginationJson))
        {
            pagination = JsonSerializer.Deserialize<GatewayCrudPaginationParams>(request.PaginationJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true });
        }

        var page = pagination?.Page ?? 1;
        var pageSize = pagination?.PageSize ?? 25;
        var sortBy = pagination?.SortBy;
        var sortDir = pagination?.SortDir ?? "asc";
        var after = pagination?.After;
        var filters = pagination?.Filters is not null
            ? new Dictionary<string, string>(pagination.Filters, StringComparer.OrdinalIgnoreCase)
            : null;

        if (!string.IsNullOrWhiteSpace(after))
        {
            var cursorQuery = new CursorPaginationQuery(pageSize, after, sortBy, sortDir);
            return await query.ToCursorPagedResultAsync(cursorQuery, filters, ct);
        }

        var paginationQuery = new PaginationQuery(page, pageSize, sortBy, sortDir);
        return await query.ToPagedResultAsync(paginationQuery, filters, ct);
    }

    private async Task<GatewayCrudResponse> HandleGetById(
        EverloreDbContext dbContext, Type entityType, GatewayCrudRequest request, CancellationToken ct)
    {
        if (!request.EntityId.HasValue)
            return new GatewayCrudResponse(request.RequestId, false, null, 400, "EntityId required for GetById");

        var entity = await dbContext.FindAsync(entityType, [request.EntityId.Value], ct);

        if (entity is null)
            return new GatewayCrudResponse(request.RequestId, false, null, 404, "Entity not found");

        var json = JsonSerializer.Serialize(entity, entityType, JsonOptions);
        return new GatewayCrudResponse(request.RequestId, true, json, 200, null);
    }

    private async Task<GatewayCrudResponse> HandleCreate(
        EverloreDbContext dbContext, Type entityType, GatewayCrudRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EntityJson))
            return new GatewayCrudResponse(request.RequestId, false, null, 400, "EntityJson required for Create");

        var entity = (BaseEntity)JsonSerializer.Deserialize(request.EntityJson, entityType, JsonOptions)!;

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        dbContext.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        var json = JsonSerializer.Serialize(entity, entityType, JsonOptions);
        return new GatewayCrudResponse(request.RequestId, true, json, 201, null);
    }

    private async Task<GatewayCrudResponse> HandleUpdate(
        EverloreDbContext dbContext, Type entityType, GatewayCrudRequest request, CancellationToken ct)
    {
        if (!request.EntityId.HasValue || string.IsNullOrWhiteSpace(request.EntityJson))
            return new GatewayCrudResponse(request.RequestId, false, null, 400, "EntityId and EntityJson required for Update");

        var existing = await dbContext.FindAsync(entityType, [request.EntityId.Value], ct);
        if (existing is null)
            return new GatewayCrudResponse(request.RequestId, false, null, 404, "Entity not found");

        var incoming = (BaseEntity)JsonSerializer.Deserialize(request.EntityJson, entityType, JsonOptions)!;

        // Preserve audit fields
        var existingEntity = (BaseEntity)existing;
        var createdAt = existingEntity.CreatedAt;
        var createdBy = existingEntity.CreatedBy;

        dbContext.Entry(existing).CurrentValues.SetValues(incoming);

        existingEntity.Id = request.EntityId.Value;
        existingEntity.CreatedAt = createdAt;
        existingEntity.CreatedBy = createdBy;

        await dbContext.SaveChangesAsync(ct);
        return new GatewayCrudResponse(request.RequestId, true, null, 204, null);
    }

    private async Task<GatewayCrudResponse> HandleDelete(
        EverloreDbContext dbContext, Type entityType, GatewayCrudRequest request, CancellationToken ct)
    {
        if (!request.EntityId.HasValue)
            return new GatewayCrudResponse(request.RequestId, false, null, 400, "EntityId required for Delete");

        var entity = await dbContext.FindAsync(entityType, [request.EntityId.Value], ct);
        if (entity is null)
            return new GatewayCrudResponse(request.RequestId, false, null, 404, "Entity not found");

        dbContext.Remove(entity);
        await dbContext.SaveChangesAsync(ct);
        return new GatewayCrudResponse(request.RequestId, true, null, 204, null);
    }
}
