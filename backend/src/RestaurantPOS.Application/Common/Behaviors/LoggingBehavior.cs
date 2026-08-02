using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Common.Behaviors;

/// <summary>
/// Records who ran which use case and whether it succeeded. On a single-site install this log
/// is the primary audit trail, so it names the acting user but never the request payload,
/// which would contain passwords and PINs.
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var actor = currentUser.Username ?? "anonymous";
        var timer = Stopwatch.StartNew();

        var response = await next(cancellationToken);

        timer.Stop();

        if (response is Result { IsFailure: true } failure)
        {
            RequestFailed(logger, requestName, actor, failure.Error.Code, timer.ElapsedMilliseconds);
        }
        else
        {
            RequestSucceeded(logger, requestName, actor, timer.ElapsedMilliseconds);
        }

        return response;
    }

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "{RequestName} handled for {Actor} in {ElapsedMs}ms")]
    private static partial void RequestSucceeded(
        ILogger logger, string requestName, string actor, long elapsedMs);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "{RequestName} rejected for {Actor} with {ErrorCode} in {ElapsedMs}ms")]
    private static partial void RequestFailed(
        ILogger logger, string requestName, string actor, string errorCode, long elapsedMs);
}