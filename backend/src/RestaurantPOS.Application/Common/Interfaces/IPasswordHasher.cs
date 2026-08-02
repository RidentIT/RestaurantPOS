namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>Hashes and verifies passwords and approval PINs.</summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted, slow hash suitable for storage.</summary>
    string Hash(string plaintext);

    /// <summary>
    /// Verifies a plaintext value against a stored hash in constant time with respect to the
    /// hash contents.
    /// </summary>
    bool Verify(string plaintext, string hash);
}