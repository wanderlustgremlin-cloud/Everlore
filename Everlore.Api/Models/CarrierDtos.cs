namespace Everlore.Api.Models;

public record CreateCarrierRequest(
    string Name,
    string Code,
    string? ContactEmail,
    string? Phone,
    bool IsActive = true);

public record UpdateCarrierRequest(
    string Name,
    string Code,
    string? ContactEmail,
    string? Phone,
    bool IsActive);

public record CarrierResponse(
    Guid Id,
    string Name,
    string Code,
    string? ContactEmail,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
