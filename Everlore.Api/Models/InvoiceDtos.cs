namespace Everlore.Api.Models;

public record CreateInvoiceRequest(
    Guid CustomerId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status,
    Guid? SalesOrderId);

public record UpdateInvoiceRequest(
    Guid CustomerId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status,
    Guid? SalesOrderId);

public record InvoiceResponse(
    Guid Id,
    Guid CustomerId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status,
    Guid? SalesOrderId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
