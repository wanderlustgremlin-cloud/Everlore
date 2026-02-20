namespace Everlore.Api.Models;

public record CreateBillRequest(
    Guid VendorId,
    string BillNumber,
    DateTime BillDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status);

public record UpdateBillRequest(
    Guid VendorId,
    string BillNumber,
    DateTime BillDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status);

public record BillResponse(
    Guid Id,
    Guid VendorId,
    string BillNumber,
    DateTime BillDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
