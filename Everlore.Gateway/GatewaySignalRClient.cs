using Everlore.Gateway.Configuration;
using Everlore.Gateway.Contracts.Messages;
using Everlore.Gateway.Handlers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;

namespace Everlore.Gateway;

public class GatewaySignalRClient(
    IOptions<GatewaySettings> settings,
    ExecuteQueryHandler executeQueryHandler,
    DiscoverSchemaHandler discoverSchemaHandler,
    ILogger<GatewaySignalRClient> logger) : IAsyncDisposable
{
    private readonly GatewaySettings _settings = settings.Value;
    private HubConnection? _connection;
    private CancellationTokenSource? _heartbeatCts;

    public async Task ConnectAsync(CancellationToken ct)
    {
        var hubUrl = $"{_settings.ServerUrl.TrimEnd('/')}/hubs/gateway";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new RetryPolicy(_settings.ReconnectDelaySeconds))
            .Build();

        RegisterHandlers();
        RegisterReconnectionEvents();

        logger.LogInformation("Connecting to gateway hub at {HubUrl}", hubUrl);
        await _connection.StartAsync(ct);
        logger.LogInformation("Connected to gateway hub (ConnectionId: {ConnectionId})", _connection.ConnectionId);

        var authenticated = await _connection.InvokeAsync<bool>("Authenticate", _settings.ApiKey, ct);
        if (!authenticated)
        {
            logger.LogCritical("Gateway authentication failed. Check your API key.");
            throw new InvalidOperationException("Gateway authentication failed.");
        }

        logger.LogInformation("Gateway agent authenticated successfully");

        _heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = SendHeartbeatLoopAsync(_heartbeatCts.Token);
    }

    private void RegisterHandlers()
    {
        var connection = _connection!;

        connection.On<GatewayExecuteQueryRequest>("ExecuteQuery", async request =>
        {
            var response = await executeQueryHandler.HandleAsync(request, CancellationToken.None);
            await connection.InvokeAsync("SendQueryResult", response);
        });

        connection.On<GatewayDiscoverSchemaRequest>("DiscoverSchema", async request =>
        {
            var response = await discoverSchemaHandler.HandleAsync(request, CancellationToken.None);
            await connection.InvokeAsync("SendSchemaResult", response);
        });

        connection.On("Ping", () =>
        {
            logger.LogDebug("Received ping from server");
        });
    }

    private void RegisterReconnectionEvents()
    {
        _connection!.Reconnecting += error =>
        {
            logger.LogWarning(error, "Connection lost. Attempting to reconnect...");
            return Task.CompletedTask;
        };

        _connection.Reconnected += async connectionId =>
        {
            logger.LogInformation("Reconnected with ConnectionId: {ConnectionId}. Re-authenticating...", connectionId);

            var authenticated = await _connection.InvokeAsync<bool>("Authenticate", _settings.ApiKey);
            if (!authenticated)
            {
                logger.LogError("Re-authentication failed after reconnection");
                return;
            }

            logger.LogInformation("Re-authenticated successfully after reconnection");
        };

        _connection.Closed += error =>
        {
            if (error is not null)
                logger.LogError(error, "Connection closed with error");
            else
                logger.LogInformation("Connection closed");

            return Task.CompletedTask;
        };
    }

    private async Task SendHeartbeatLoopAsync(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(_settings.HeartbeatIntervalSeconds);
        var dataSourceIds = _settings.DataSources.Values
            .Select(ds => ds.DataSourceId)
            .ToList();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);

                if (_connection?.State == HubConnectionState.Connected)
                {
                    var heartbeat = new GatewayHeartbeat(
                        GetAgentVersion(),
                        dataSourceIds,
                        DateTime.UtcNow);

                    await _connection.InvokeAsync("Heartbeat", heartbeat, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send heartbeat");
            }
        }
    }

    private static string GetAgentVersion()
    {
        return typeof(GatewaySignalRClient).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    }

    public async ValueTask DisposeAsync()
    {
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class RetryPolicy(int baseDelaySeconds) : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            // Exponential backoff with jitter, max 60s
            var delay = Math.Min(
                baseDelaySeconds * Math.Pow(2, retryContext.PreviousRetryCount),
                60);
            var jitter = Random.Shared.NextDouble() * delay * 0.2;
            return TimeSpan.FromSeconds(delay + jitter);
        }
    }
}
