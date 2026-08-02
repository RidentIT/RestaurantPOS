using System.Security.Cryptography;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>
/// Supplies the symmetric key used to sign and validate access tokens.
/// </summary>
/// <remarks>
/// This system is installed on a single machine in the restaurant, so requiring the installer
/// to invent and configure a secret would be friction that ends in a weak shared default.
/// Instead a strong key is generated on first run and persisted next to the application. An
/// explicitly configured <see cref="JwtOptions.SigningKey"/> always wins, which is what a
/// multi-machine or containerised deployment would use.
/// </remarks>
public sealed class JwtSigningKeyProvider
{
    private const int KeySizeBytes = 64;

    public JwtSigningKeyProvider(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var settings = options.Value;

        var keyMaterial = !string.IsNullOrWhiteSpace(settings.SigningKey)
            ? Convert.FromBase64String(settings.SigningKey)
            : LoadOrCreatePersistedKey(settings.KeyFilePath);

        if (keyMaterial.Length < 32)
        {
            throw new InvalidOperationException(
                "The configured JWT signing key is too short. Supply at least 32 bytes of base64-encoded material.");
        }

        SecurityKey = new SymmetricSecurityKey(keyMaterial);
    }

    public SymmetricSecurityKey SecurityKey { get; }

    private static byte[] LoadOrCreatePersistedKey(string keyFilePath)
    {
        var path = Path.IsPathRooted(keyFilePath)
            ? keyFilePath
            : Path.Combine(AppContext.BaseDirectory, keyFilePath);

        if (File.Exists(path))
        {
            return Convert.FromBase64String(File.ReadAllText(path).Trim());
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var generated = RandomNumberGenerator.GetBytes(KeySizeBytes);
        File.WriteAllText(path, Convert.ToBase64String(generated));

        return generated;
    }
}