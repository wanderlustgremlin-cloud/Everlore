namespace Everlore.Infrastructure.Tenancy;

public interface ITenantProvider
{
    string? GetTenantIdentifier();
}
