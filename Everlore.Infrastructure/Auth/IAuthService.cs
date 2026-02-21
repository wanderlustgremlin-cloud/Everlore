using Everlore.Domain.Tenancy;

namespace Everlore.Infrastructure.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> SelectTenantAsync(Guid userId, Guid tenantId);
    Task<AuthResult> RegisterAsync(string email, string password, string fullName);
}

public record AuthResult(
    bool Succeeded,
    string? Token = null,
    string? Error = null,
    IReadOnlyList<TenantInfo>? Tenants = null);

public record TenantInfo(Guid Id, string Name, string Identifier, string Role);
