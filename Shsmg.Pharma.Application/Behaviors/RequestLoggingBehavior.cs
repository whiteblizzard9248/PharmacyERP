using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shsmg.Pharma.Application.Behaviors;

public sealed class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = new Activity(requestName);
        activity.Start();

        _logger.LogInformation("Handling {RequestName} {@Request}", requestName, request);

        var response = await next();

        activity.Stop();
        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMilliseconds}ms",
            requestName,
            activity.Duration.TotalMilliseconds);

        return response;
    }
}
