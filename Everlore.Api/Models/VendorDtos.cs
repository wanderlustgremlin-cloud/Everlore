namespace Everlore.Api.Models;

public record CreateVendorRequest(
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    bool IsActive = true);

public record UpdateVendorRequest(
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    bool IsActive);

public record VendorResponse(
    Guid Id,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
