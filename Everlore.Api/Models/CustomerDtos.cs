namespace Everlore.Api.Models;

public record CreateCustomerRequest(
    string Name,
    string? ContactEmail,
    string? Phone,
    string? BillingAddress,
    bool IsActive = true);

public record UpdateCustomerRequest(
    string Name,
    string? ContactEmail,
    string? Phone,
    string? BillingAddress,
    bool IsActive);

public record CustomerResponse(
    Guid Id,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? BillingAddress,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
