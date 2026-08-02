using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Infrastructure.Persistence.Seeding;

/// <summary>
/// Brings a fresh database up to a usable state: applies migrations and guarantees that at
/// least one administrator exists.
/// </summary>
public sealed partial class DatabaseSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IOptions<SeedAdminOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    private readonly SeedAdminOptions _options = options.Value;

    /// <summary>
    /// Applies pending migrations, then creates the built-in administrator if no administrator
    /// account exists. Safe to run on every start-up.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        var anyAdmin = await db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken);
        if (anyAdmin)
        {
            return;
        }

        var username = _options.Username.Trim().ToLowerInvariant();

        // Guard against a non-admin already holding the configured name, which would otherwise
        // fail the unique index and stop the application from starting.
        var nameTaken = await db.Users.AnyAsync(u => u.Username == username, cancellationToken);
        if (nameTaken)
        {
            SeedUsernameTaken(logger, username);
            return;
        }

        var admin = User.CreateSystemAdmin(
            username,
            _options.FullName,
            passwordHasher.Hash(_options.Password));

        db.Users.Add(admin);
        await db.SaveChangesAsync(cancellationToken);

        SeededAdmin(logger, username);
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "Seeded the built-in administrator '{Username}'. " +
                  "It must change its password at first sign-in.")]
    private static partial void SeededAdmin(ILogger logger, string username);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Error,
        Message = "Cannot seed an administrator: the username '{Username}' is already taken by a " +
                  "non-administrator. Set SeedAdmin:Username to a free name.")]
    private static partial void SeedUsernameTaken(ILogger logger, string username);
}