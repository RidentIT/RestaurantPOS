using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Security;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>Issues signed access tokens and opaque refresh tokens.</summary>
internal sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    JwtSigningKeyProvider keyProvider,
    IDateTimeProvider clock) : ITokenService
{
    private const int RefreshTokenSizeBytes = 32;

    private readonly JwtOptions _options = options.Value;

    public AccessToken CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        // Administrators are authorised by role, so listing every module would only bloat the
        // token. Regular users carry one claim per granted module.
        if (user.Role != UserRole.Admin)
        {
            claims.AddRange(user
                .EffectiveModules()
                .Select(m => new Claim(AppClaimTypes.Module, m.ToString())));
        }

        var credentials = new SigningCredentials(keyProvider.SecurityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public RefreshTokenPair CreateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenSizeBytes));

        return new RefreshTokenPair(
            value,
            HashRefreshToken(value),
            clock.UtcNow.AddDays(_options.RefreshTokenDays));
    }

    /// <summary>
    /// A plain SHA-256 digest is enough here: the token is 256 bits of cryptographic randomness,
    /// so there is no low-entropy input for an attacker to brute force the way there is with a
    /// password. Using BCrypt instead would only slow every request down.
    /// </summary>
    public string HashRefreshToken(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(digest).ToLowerInvariant();
    }
}