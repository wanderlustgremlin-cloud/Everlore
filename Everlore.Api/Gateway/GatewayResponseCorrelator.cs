using System.Collections.Concurrent;
using Everlore.Gateway.Contracts.Messages;

namespace Everlore.Api.Gateway;

public class GatewayResponseCorrelator : IGatewayResponseCorrelator
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayExecuteQueryResponse>> _queryWaiters = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayDiscoverSchemaResponse>> _schemaWaiters = new();
    private readonly ConcurrentDictionary<string, Guid> _requestToTenant = new();

    public async Task<GatewayExecuteQueryResponse> WaitForQueryResponseAsync(
        string requestId, TimeSpan timeout, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<GatewayExecuteQueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queryWaiters[requestId] = tcs;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            await using var registration = cts.Token.Register(() =>
                tcs.TrySetCanceled(cts.Token));

            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Gateway agent did not respond within {timeout.TotalSeconds}s for request {requestId}.");
        }
        finally
        {
            _queryWaiters.TryRemove(requestId, out _);
            _requestToTenant.TryRemove(requestId, out _);
        }
    }

    public async Task<GatewayDiscoverSchemaResponse> WaitForSchemaResponseAsync(
        string requestId, TimeSpan timeout, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<GatewayDiscoverSchemaResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _schemaWaiters[requestId] = tcs;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            await using var registration = cts.Token.Register(() =>
                tcs.TrySetCanceled(cts.Token));

            return await tcs.Task;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Gateway agent did not respond within {timeout.TotalSeconds}s for request {requestId}.");
        }
        finally
        {
            _schemaWaiters.TryRemove(requestId, out _);
            _requestToTenant.TryRemove(requestId, out _);
        }
    }

    public void CompleteQueryResponse(string requestId, GatewayExecuteQueryResponse response)
    {
        if (_queryWaiters.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }

    public void CompleteSchemaResponse(string requestId, GatewayDiscoverSchemaResponse response)
    {
        if (_schemaWaiters.TryRemove(requestId, out var tcs))
        {
            tcs.TrySetResult(response);
        }
    }

    public void FailRequest(string requestId, string error)
    {
        if (_queryWaiters.TryRemove(requestId, out var queryTcs))
        {
            queryTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
        }

        if (_schemaWaiters.TryRemove(requestId, out var schemaTcs))
        {
            schemaTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
        }

        _requestToTenant.TryRemove(requestId, out _);
    }

    public void RegisterRequestTenant(string requestId, Guid tenantId)
    {
        _requestToTenant[requestId] = tenantId;
    }

    public void CancelPendingRequests(Guid tenantId)
    {
        var requestIds = _requestToTenant
            .Where(kvp => kvp.Value == tenantId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var requestId in requestIds)
        {
            FailRequest(requestId, "Gateway agent disconnected.");
        }
    }
}
