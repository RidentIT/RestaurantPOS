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
        await SeedExpenseCategoriesAsync(cancellationToken);

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

    /// <summary>
    /// The expense categories a restaurant starts with (EXP-010), each with the budget from the
    /// requirements. Only ever added when missing by name, so renaming "Gas/LPG" to something the
    /// staff prefer does not cause it to be recreated on the next start-up.
    /// </summary>
    private async Task SeedExpenseCategoriesAsync(CancellationToken cancellationToken)
    {
        (string Name, string Description, decimal Budget)[] defaults =
        [
            ("Rent/Lease", "Monthly rent for the premises", 50_000m),
            ("Electricity", "CEB power bills", 35_000m),
            ("Gas/LPG", "Cooking gas cylinders", 30_000m),
            ("Water/Waste", "Water supply and waste collection", 20_000m),
            ("Staff Meals", "Meals provided to employees", 15_000m),
            ("Salaries", "Staff wages", 180_000m),
            ("Maintenance", "Repairs to equipment and premises", 25_000m),
            ("Miscellaneous", "Everything without a category of its own", 50_000m),
        ];

        var existing = await db.ExpenseCategories
            .Select(c => c.Name.ToLower())
            .ToListAsync(cancellationToken);

        var missing = defaults
            .Where(d => !existing.Contains(d.Name.ToLowerInvariant()))
            .Select(d => ExpenseCategory.Create(d.Name, d.Description, d.Budget, parentCategoryId: null, isSystem: true))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.ExpenseCategories.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);

        SeededExpenseCategories(logger, missing.Count);
    }

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Seeded {Count} built-in expense categories.")]
    private static partial void SeededExpenseCategories(ILogger logger, int count);

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