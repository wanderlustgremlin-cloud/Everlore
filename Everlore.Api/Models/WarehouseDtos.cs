namespace Everlore.Api.Models;

public record CreateWarehouseRequest(
    string Name,
    string Code,
    string? Address,
    bool IsActive = true);

public record UpdateWarehouseRequest(
    string Name,
    string Code,
    string? Address,
    bool IsActive);

public record WarehouseResponse(
    Guid Id,
    string Name,
    string Code,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
