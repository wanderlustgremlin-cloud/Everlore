using Everlore.Gateway.Contracts.Messages;

namespace Everlore.Api.Gateway;

public interface IGatewayResponseCorrelator
{
    Task<GatewayExecuteQueryResponse> WaitForQueryResponseAsync(string requestId, TimeSpan timeout, CancellationToken ct = default);
    Task<GatewayDiscoverSchemaResponse> WaitForSchemaResponseAsync(string requestId, TimeSpan timeout, CancellationToken ct = default);
    void CompleteQueryResponse(string requestId, GatewayExecuteQueryResponse response);
    void CompleteSchemaResponse(string requestId, GatewayDiscoverSchemaResponse response);
    void FailRequest(string requestId, string error);
    void CancelPendingRequests(Guid tenantId);
    void RegisterRequestTenant(string requestId, Guid tenantId);
}
