namespace Everlore.Api.Models;

public record CreateShipmentRequest(
    Guid CarrierId,
    Guid? SalesOrderId,
    string? TrackingNumber,
    string Status,
    DateTime? ShippedDate,
    DateTime? DeliveredDate,
    string? ShipToAddress);

public record UpdateShipmentRequest(
    Guid CarrierId,
    Guid? SalesOrderId,
    string? TrackingNumber,
    string Status,
    DateTime? ShippedDate,
    DateTime? DeliveredDate,
    string? ShipToAddress);

public record ShipmentResponse(
    Guid Id,
    Guid CarrierId,
    Guid? SalesOrderId,
    string? TrackingNumber,
    string Status,
    DateTime? ShippedDate,
    DateTime? DeliveredDate,
    string? ShipToAddress,
    DateTime CreatedAt,
    DateTime UpdatedAt);
