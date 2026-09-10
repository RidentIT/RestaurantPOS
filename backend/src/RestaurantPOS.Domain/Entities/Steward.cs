using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A member of the waiting staff an order can be credited to — the person who actually served the
/// table, as opposed to <see cref="Order.CashierUserId"/>, whoever happened to key the bill in.
/// </summary>
/// <remarks>
/// Deliberately not a <see cref="User"/>: stewards do not sign in, hold no permissions and are not
/// accounts. They are a short list of names an administrator maintains so the till can attribute a
/// table to one of them and the owner can see sales per steward. Kept, never hard-deleted — a
/// steward who leaves is deactivated so the orders they served keep their name on the report.
/// </remarks>
public sealed class Steward : BaseEntity
{
    public const int NameMaxLength = 80;

    // EF Core materialisation.
    private Steward()
    {
    }

    private Steward(string name)
    {
        Name = Normalise(name);
        IsActive = true;
    }

    /// <summary>The steward's name as the staff say it — first name, or however the roster reads.</summary>
    public string Name { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Steward Create(string name) => new(name);

    public void Rename(string name) => Name = Normalise(name);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string Normalise(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();

        return trimmed.Length > NameMaxLength
            ? throw new ArgumentException($"A steward's name cannot exceed {NameMaxLength} characters.", nameof(name))
            : trimmed;
    }
}
