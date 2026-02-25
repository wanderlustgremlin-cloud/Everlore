namespace Everlore.Gateway;

public class GatewayWorker(
    GatewaySignalRClient signalRClient,
    ILogger<GatewayWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Gateway agent starting...");

        try
        {
            await signalRClient.ConnectAsync(stoppingToken);

            // Keep the service running until shutdown is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Gateway agent shutting down gracefully");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Gateway agent encountered a fatal error");
            throw;
        }
        finally
        {
            await signalRClient.DisposeAsync();
        }
    }
}
