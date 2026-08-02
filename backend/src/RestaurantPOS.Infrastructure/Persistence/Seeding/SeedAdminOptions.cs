namespace RestaurantPOS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Credentials for the built-in administrator created on a fresh database, bound from the
/// <c>SeedAdmin</c> configuration section.
/// </summary>
/// <remarks>
/// The seeded account is always flagged to change its password at first sign-in, so the value
/// configured here is a one-time bootstrap credential rather than a lasting password.
/// </remarks>
public sealed class SeedAdminOptions
{
    public const string SectionName = "SeedAdmin";

    public string Username { get; set; } = "admin";

    public string FullName { get; set; } = "System Administrator";

    /// <summary>
    /// Bootstrap password. Override it per installation via configuration or the
    /// <c>SeedAdmin__Password</c> environment variable.
    /// </summary>
    public string Password { get; set; } = "ChangeMe!123";
}