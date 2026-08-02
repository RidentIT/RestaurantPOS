using RestaurantPOS.Application.Users.Dtos;

namespace RestaurantPOS.Application.Authentication.Dtos;

/// <summary>Everything a client needs to establish a signed-in session.</summary>
public sealed record AuthenticationResult
{
    /// <summary>Short-lived signed JWT sent on every subsequent request.</summary>
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpiresAtUtc { get; init; }

    /// <summary>Long-lived opaque token used to obtain a new access token.</summary>
    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpiresAtUtc { get; init; }

    /// <summary>
    /// The signed-in user. When <see cref="UserDto.MustChangePassword"/> is true the session is
    /// restricted to the password-change endpoints until a new password is chosen.
    /// </summary>
    public required UserDto User { get; init; }
}