using System.Net;
using System.Text.Json;

namespace RestaurantPOS.API.Middleware;

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
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            LogUnhandledException(_logger, "Unhandled exception occurred", ex);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                statusCode = context.Response.StatusCode,
                message = "An unexpected error occurred. Please try again later."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}