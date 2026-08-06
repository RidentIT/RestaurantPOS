namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A single issued refresh token. Only a hash of the token is stored, so a leak of the
/// database does not hand an attacker usable sessions.
/// </summary>
public sealed class RefreshToken
{
    // EF Core materialisation.
    private RefreshToken()
    {
    }

    internal RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = nowUtc;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>SHA-256 hash of the opaque token handed to the client.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    /// <summary>True when the token has neither been revoked nor expired.</summary>
    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    internal void Revoke(DateTime nowUtc) => RevokedAtUtc ??= nowUtc;
}