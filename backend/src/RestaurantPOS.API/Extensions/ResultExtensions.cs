using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.API.Extensions;

/// <summary>
/// Translates the application layer's <see cref="Result"/> into HTTP responses, so endpoints
/// never decide status codes for themselves and every failure looks the same on the wire.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Maps a valueless result to 204 No Content, or an RFC 7807 problem on failure.</summary>
    public static IResult ToHttpResult(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Results.NoContent() : Problem(result.Error);
    }

    /// <summary>Maps a result to 200 OK with its value, or an RFC 7807 problem on failure.</summary>
    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);
    }

    /// <summary>Maps a successful result to 201 Created at <paramref name="location"/>.</summary>
    public static IResult ToCreatedResult<TValue>(this Result<TValue> result, Func<TValue, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);

        return result.IsSuccess
            ? Results.Created(location(result.Value), result.Value)
            : Problem(result.Error);
    }

    private static IResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Problem(
            title: error.Description,
            statusCode: statusCode,
            // The stable machine-readable code lets the client branch on a specific failure
            // (for example, routing to the password-reset screen) without matching on prose.
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}