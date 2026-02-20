namespace Everlore.Api.Models;

public record CreateSalesOrderRequest(
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount);

public record UpdateSalesOrderRequest(
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount);

public record SalesOrderResponse(
    Guid Id,
    Guid CustomerId,
    string OrderNumber,
    DateTime OrderDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
