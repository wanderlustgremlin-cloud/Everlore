namespace Everlore.Api.Models;

public record LoginRequest(string Email, string Password);
public record SelectTenantRequest(Guid TenantId);
public record RegisterRequest(string Email, string Password, string FullName);

public record AuthResponse(bool Succeeded, string? Token, string? Error, IReadOnlyList<TenantInfoResponse>? Tenants);
public record TenantInfoResponse(Guid Id, string Name, string Identifier, string Role);
