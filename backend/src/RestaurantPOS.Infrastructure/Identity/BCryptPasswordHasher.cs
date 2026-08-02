using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>
/// BCrypt-based hashing for passwords and approval PINs. The work factor makes offline
/// guessing expensive, which matters most for the 4-digit PIN with its tiny key space.
/// </summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Cost 12 is roughly 250ms per hash on typical till hardware — slow enough to blunt
    /// brute force, fast enough to keep sign-in snappy.
    /// </summary>
    private const int WorkFactor = 12;

    public string Hash(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);

        return BCrypt.Net.BCrypt.HashPassword(plaintext, WorkFactor);
    }

    public bool Verify(string plaintext, string hash)
    {
        if (string.IsNullOrEmpty(plaintext) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(plaintext, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // A malformed stored hash must read as "wrong password", never as an unhandled 500.
            return false;
        }
    }
}