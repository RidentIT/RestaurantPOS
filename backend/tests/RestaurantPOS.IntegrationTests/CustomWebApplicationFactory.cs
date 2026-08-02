using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RestaurantPOS.IntegrationTests;

/// <summary>
/// Boots the real API against a throwaway SQLite file.
/// </summary>
/// <remarks>
/// The application's own start-up path runs the migrations and seeds the administrator, so
/// these tests exercise the same bootstrap a fresh install goes through rather than a
/// test-only shortcut.
/// </remarks>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string SeedAdminUsername = "admin";

    /// <summary>The bootstrap password the seeded administrator is created with.</summary>
    public const string SeedAdminPassword = "Bootstrap@2026";

    private readonly string _dbFilePath =
        Path.Combine(Path.GetTempPath(), $"restaurantpos-test-{Guid.NewGuid()}.db");

    private readonly string _keyFilePath =
        Path.Combine(Path.GetTempPath(), $"restaurantpos-test-{Guid.NewGuid()}.key");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbFilePath}",
                ["SeedAdmin:Username"] = SeedAdminUsername,
                ["SeedAdmin:Password"] = SeedAdminPassword,
                ["SeedAdmin:FullName"] = "System Administrator",
                // Each factory gets its own signing key so tokens never leak between test classes.
                ["Jwt:KeyFilePath"] = _keyFilePath,
            }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var path in new[] { _dbFilePath, _keyFilePath })
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // Transient file locks on Windows are not worth failing a test run over.
            }
            catch (UnauthorizedAccessException)
            {
                // As above.
            }
        }
    }
}