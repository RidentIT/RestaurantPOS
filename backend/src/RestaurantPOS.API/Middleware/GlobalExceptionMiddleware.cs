using Microsoft.AspNetCore.Mvc;

using RestaurantPOS.Application.Common.Exceptions;

namespace RestaurantPOS.API.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 problem responses, matching the shape produced
/// by <see cref="Extensions.ResultExtensions"/> so clients only parse one error format.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    private static readonly Action<ILogger, string, Exception?> LogUnhandledException =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, nameof(InvokeAsync)), "{Message}");

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context);
        }
        catch (ValidationAppException ex)
        {
            // Expected control flow for bad input, so it is logged at debug rather than error.
            await WriteProblemAsync(context, new ValidationProblemDetails(
                ex.Errors.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal))
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
            });
        }
        catch (Exception ex)
        {
            LogUnhandledException(_logger, "Unhandled exception occurred", ex);

            await WriteProblemAsync(context, new ProblemDetails
            {
                Title = "An unexpected error occurred. Please try again later.",
                Status = StatusCodes.Status500InternalServerError,
            });
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, ProblemDetails problem)
    {
        if (context.Response.HasStarted)
        {
            // Too late to change the response; swallowing here avoids masking the original error.
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problem, problem.GetType());
    }
}