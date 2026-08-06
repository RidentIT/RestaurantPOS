using RestaurantPOS.Application.Authentication.Dtos;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Authentication.Common;

/// <summary>
/// Builds a signed-in session for a user. Shared by sign-in, token refresh and password change
/// so all three issue tokens identically.
/// </summary>
internal static class SessionFactory
{
    /// <summary>
    /// Mints an access/refresh token pair and records the refresh token on the user. The caller
    /// is responsible for persisting the change.
    /// </summary>
    public static AuthenticationResult Issue(User user, ITokenService tokens, IDateTimeProvider clock)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();

        user.IssueRefreshToken(refresh.Hash, refresh.ExpiresAtUtc, clock.UtcNow);

        return new AuthenticationResult
        {
            AccessToken = access.Value,
            AccessTokenExpiresAtUtc = access.ExpiresAtUtc,
            RefreshToken = refresh.Value,
            RefreshTokenExpiresAtUtc = refresh.ExpiresAtUtc,
            User = user.ToDto(),
        };
    }
}