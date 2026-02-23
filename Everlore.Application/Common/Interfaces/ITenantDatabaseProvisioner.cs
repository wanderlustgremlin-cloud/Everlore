namespace Everlore.Application.Common.Interfaces;

public interface ITenantDatabaseProvisioner
{
    Task<string> ProvisionAsync(string tenantIdentifier, CancellationToken ct = default);
}
