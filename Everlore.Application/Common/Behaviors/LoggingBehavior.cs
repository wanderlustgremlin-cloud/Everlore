using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Everlore.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > 500)
        {
            logger.LogWarning("Long running request: {RequestName} ({ElapsedMs}ms)", requestName, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
