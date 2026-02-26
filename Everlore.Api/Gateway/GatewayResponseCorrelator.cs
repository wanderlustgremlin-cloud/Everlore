using System.Collections.Concurrent;
using Everlore.Gateway.Contracts.Messages;
using Microsoft.Extensions.Logging;

namespace Everlore.Api.Gateway;

public class GatewayResponseCorrelator(ILogger<GatewayResponseCorrelator> logger) : IGatewayResponseCorrelator
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayExecuteQueryResponse>> _queryWaiters = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayDiscoverSchemaResponse>> _schemaWaiters = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayExploreResponse>> _exploreWaiters = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<GatewayCrudResponse>> _crudWaiters = new();
    private readonly ConcurrentDictionary<string, Guid> _requestToTenant = new();

    public async Task<GatewayExecuteQueryResponse> WaitForQueryResponseAsync(
        string requestId, TimeSpan timeout, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<GatewayExecuteQueryResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _queryWaiters[requestId] = tcs;

        logger.LogInformation("Registered query waiter for request {RequestId} (timeout={TimeoutSeconds}s)",
            requestId, timeout.TotalSeconds);

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
            logger.LogWarning("Query request {RequestId} timed out after {TimeoutSeconds}s",
                requestId, timeout.TotalSeconds);
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

        logger.LogInformation("Registered schema waiter for request {RequestId} (timeout={TimeoutSeconds}s)",
            requestId, timeout.TotalSeconds);

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
            logger.LogWarning("Schema request {RequestId} timed out after {TimeoutSeconds}s",
                requestId, timeout.TotalSeconds);
            throw new TimeoutException($"Gateway agent did not respond within {timeout.TotalSeconds}s for request {requestId}.");
        }
        finally
        {
            _schemaWaiters.TryRemove(requestId, out _);
            _requestToTenant.TryRemove(requestId, out _);
        }
    }

    public async Task<GatewayExploreResponse> WaitForExploreResponseAsync(
        string requestId, TimeSpan timeout, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<GatewayExploreResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _exploreWaiters[requestId] = tcs;

        logger.LogInformation("Registered explore waiter for request {RequestId} (timeout={TimeoutSeconds}s)",
            requestId, timeout.TotalSeconds);

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
            logger.LogWarning("Explore request {RequestId} timed out after {TimeoutSeconds}s",
                requestId, timeout.TotalSeconds);
            throw new TimeoutException($"Gateway agent did not respond within {timeout.TotalSeconds}s for request {requestId}.");
        }
        finally
        {
            _exploreWaiters.TryRemove(requestId, out _);
            _requestToTenant.TryRemove(requestId, out _);
        }
    }

    public void CompleteQueryResponse(string requestId, GatewayExecuteQueryResponse response)
    {
        if (_queryWaiters.TryRemove(requestId, out var tcs))
        {
            logger.LogInformation("Completed query response for request {RequestId}", requestId);
            tcs.TrySetResult(response);
        }
        else
        {
            logger.LogWarning("Received orphaned query response for request {RequestId} (no waiter)", requestId);
        }
    }

    public void CompleteSchemaResponse(string requestId, GatewayDiscoverSchemaResponse response)
    {
        if (_schemaWaiters.TryRemove(requestId, out var tcs))
        {
            logger.LogInformation("Completed schema response for request {RequestId}", requestId);
            tcs.TrySetResult(response);
        }
        else
        {
            logger.LogWarning("Received orphaned schema response for request {RequestId} (no waiter)", requestId);
        }
    }

    public void CompleteExploreResponse(string requestId, GatewayExploreResponse response)
    {
        if (_exploreWaiters.TryRemove(requestId, out var tcs))
        {
            logger.LogInformation("Completed explore response for request {RequestId}", requestId);
            tcs.TrySetResult(response);
        }
        else
        {
            logger.LogWarning("Received orphaned explore response for request {RequestId} (no waiter)", requestId);
        }
    }

    public async Task<GatewayCrudResponse> WaitForCrudResponseAsync(
        string requestId, TimeSpan timeout, CancellationToken ct = default)
    {
        var tcs = new TaskCompletionSource<GatewayCrudResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _crudWaiters[requestId] = tcs;

        logger.LogInformation("Registered CRUD waiter for request {RequestId} (timeout={TimeoutSeconds}s)",
            requestId, timeout.TotalSeconds);

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
            logger.LogWarning("CRUD request {RequestId} timed out after {TimeoutSeconds}s",
                requestId, timeout.TotalSeconds);
            throw new TimeoutException($"Gateway agent did not respond within {timeout.TotalSeconds}s for request {requestId}.");
        }
        finally
        {
            _crudWaiters.TryRemove(requestId, out _);
            _requestToTenant.TryRemove(requestId, out _);
        }
    }

    public void CompleteCrudResponse(string requestId, GatewayCrudResponse response)
    {
        if (_crudWaiters.TryRemove(requestId, out var tcs))
        {
            logger.LogInformation("Completed CRUD response for request {RequestId}", requestId);
            tcs.TrySetResult(response);
        }
        else
        {
            logger.LogWarning("Received orphaned CRUD response for request {RequestId} (no waiter)", requestId);
        }
    }

    public void FailRequest(string requestId, string error)
    {
        logger.LogInformation("Failing request {RequestId}: {Error}", requestId, error);

        if (_queryWaiters.TryRemove(requestId, out var queryTcs))
        {
            queryTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
        }

        if (_schemaWaiters.TryRemove(requestId, out var schemaTcs))
        {
            schemaTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
        }

        if (_exploreWaiters.TryRemove(requestId, out var exploreTcs))
        {
            exploreTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
        }

        if (_crudWaiters.TryRemove(requestId, out var crudTcs))
        {
            crudTcs.TrySetException(new InvalidOperationException($"Gateway error: {error}"));
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

        if (requestIds.Count > 0)
        {
            logger.LogWarning("Cancelling {Count} pending requests for disconnected tenant {TenantId}",
                requestIds.Count, tenantId);
        }

        foreach (var requestId in requestIds)
        {
            FailRequest(requestId, "Gateway agent disconnected.");
        }
    }
}
