using System.Security.Cryptography;
using System.Text;
using Everlore.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Api.Gateway;

public class GatewayApiKeyValidator(ICatalogDbContext db) : IGatewayApiKeyValidator
{
    public async Task<GatewayApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return new GatewayApiKeyValidationResult(false, null, "API key is required.");

        var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

        var key = await db.GatewayApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

        if (key is null)
            return new GatewayApiKeyValidationResult(false, null, "Invalid API key.");

        if (key.IsRevoked)
            return new GatewayApiKeyValidationResult(false, null, "API key has been revoked.");

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
            return new GatewayApiKeyValidationResult(false, null, "API key has expired.");

        // Update last used timestamp
        key.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new GatewayApiKeyValidationResult(true, key.TenantId, null);
    }
}
