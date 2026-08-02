using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>A freshly minted access token and its expiry.</summary>
/// <param name="Value">The signed JWT.</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
public readonly record struct AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>An opaque refresh token, held by the client, alongside the hash to persist.</summary>
/// <param name="Value">The opaque token returned to the client. Never stored.</param>
/// <param name="Hash">Digest of <paramref name="Value"/>, safe to persist.</param>
/// <param name="ExpiresAtUtc">When the token stops being accepted.</param>
public readonly record struct RefreshTokenPair(string Value, string Hash, DateTime ExpiresAtUtc);

/// <summary>Issues and hashes the tokens that back a signed-in session.</summary>
public interface ITokenService
{
    /// <summary>Signs a JWT carrying the user's identity, role and granted modules.</summary>
    AccessToken CreateAccessToken(User user);

    /// <summary>Generates a cryptographically random refresh token and its storage hash.</summary>
    RefreshTokenPair CreateRefreshToken();

    /// <summary>Hashes a client-supplied refresh token so it can be matched against storage.</summary>
    string HashRefreshToken(string token);
}