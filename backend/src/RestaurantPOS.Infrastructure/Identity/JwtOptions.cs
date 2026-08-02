using System.ComponentModel.DataAnnotations;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>Token issuing and validation settings, bound from the <c>Jwt</c> configuration section.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "RestaurantPOS";

    public string Audience { get; set; } = "RestaurantPOS.Client";

    /// <summary>
    /// Base64 signing key. Left unset for a local install, in which case a random key is
    /// generated on first run and kept in <see cref="KeyFilePath"/>.
    /// </summary>
    public string? SigningKey { get; set; }

    /// <summary>
    /// Where the auto-generated signing key is stored. Relative paths resolve against the
    /// application directory. Deleting this file signs everyone out.
    /// </summary>
    public string KeyFilePath { get; set; } = Path.Combine("keys", "jwt-signing.key");

    /// <summary>
    /// Access token lifetime. Short by design — the client silently refreshes, and a shorter
    /// window limits how long a revoked user keeps working.
    /// </summary>
    [Range(5, 720)]
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>How long a till stays signed in without re-entering a password.</summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 14;
}