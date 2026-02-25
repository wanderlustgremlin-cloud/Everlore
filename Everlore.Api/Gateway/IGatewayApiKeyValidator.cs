namespace Everlore.Api.Gateway;

public record GatewayApiKeyValidationResult(bool IsValid, Guid? TenantId, string? Error);

public interface IGatewayApiKeyValidator
{
    Task<GatewayApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken ct = default);
}
