namespace Everlore.Api.Models;

public record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string UnitOfMeasure,
    bool IsActive = true);

public record UpdateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string UnitOfMeasure,
    bool IsActive);

public record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    string UnitOfMeasure,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
