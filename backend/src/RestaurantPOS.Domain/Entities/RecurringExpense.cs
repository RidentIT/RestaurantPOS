using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A standing monthly cost — rent, salaries — that should appear every month without being
/// keyed in again (EXP-012, BR-EXP-008).
/// </summary>
/// <remarks>
/// The generated expense arrives as a <see cref="ExpenseStatus.Draft"/>, never approved. Rent
/// changes, a month gets skipped, a salary bill differs — a standing instruction is a reminder of
/// what to expect, not permission to book money the manager has not looked at.
/// <para>
/// <see cref="LastGeneratedYear"/> and <see cref="LastGeneratedMonth"/> are what make generation
/// safe to run whenever the app is opened: a month that has already produced its expense is
/// simply skipped, so opening the screen five times in one morning cannot create five rents.
/// </para>
/// </remarks>
public sealed class RecurringExpense : BaseEntity
{
    public const int DescriptionMaxLength = 500;

    // EF Core materialisation.
    private RecurringExpense()
    {
    }

    private RecurringExpense(
        Guid categoryId,
        decimal amount,
        string? description,
        ExpensePaymentMethod paymentMethod,
        int dayOfMonth)
    {
        CategoryId = categoryId;
        Amount = ValidateAmount(amount);
        Description = NormaliseDescription(description);
        PaymentMethod = paymentMethod;
        DayOfMonth = ValidateDayOfMonth(dayOfMonth);
        IsActive = true;
    }

    public Guid CategoryId { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    public ExpensePaymentMethod PaymentMethod { get; private set; }

    /// <summary>
    /// Which day of the month the expense falls on. Clamped to the length of a short month, so a
    /// rent set to the 31st still lands on the 28th of February rather than being skipped.
    /// </summary>
    public int DayOfMonth { get; private set; }

    public bool IsActive { get; private set; }

    public int? LastGeneratedYear { get; private set; }

    public int? LastGeneratedMonth { get; private set; }

    public static RecurringExpense Create(
        Guid categoryId,
        decimal amount,
        string? description,
        ExpensePaymentMethod paymentMethod,
        int dayOfMonth) =>
        new(categoryId, amount, description, paymentMethod, dayOfMonth);

    public void UpdateDetails(
        Guid categoryId, decimal amount, string? description, ExpensePaymentMethod paymentMethod, int dayOfMonth)
    {
        CategoryId = categoryId;
        Amount = ValidateAmount(amount);
        Description = NormaliseDescription(description);
        PaymentMethod = paymentMethod;
        DayOfMonth = ValidateDayOfMonth(dayOfMonth);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>True when this instruction still owes an expense for the given month.</summary>
    public bool IsDueFor(int year, int month)
    {
        if (!IsActive)
        {
            return false;
        }

        if (LastGeneratedYear is null || LastGeneratedMonth is null)
        {
            return true;
        }

        return (year, month).CompareTo((LastGeneratedYear.Value, LastGeneratedMonth.Value)) > 0;
    }

    /// <summary>The date this month's expense falls on, clamped to the month's length.</summary>
    public DateOnly DateFor(int year, int month) =>
        new(year, month, Math.Min(DayOfMonth, DateTime.DaysInMonth(year, month)));

    public void MarkGenerated(int year, int month)
    {
        LastGeneratedYear = year;
        LastGeneratedMonth = month;
    }

    private static decimal ValidateAmount(decimal amount) =>
        amount > 0
            ? amount
            : throw new ArgumentOutOfRangeException(nameof(amount), amount, "An amount must be greater than zero.");

    private static int ValidateDayOfMonth(int dayOfMonth) =>
        dayOfMonth is >= 1 and <= 31
            ? dayOfMonth
            : throw new ArgumentOutOfRangeException(
                nameof(dayOfMonth), dayOfMonth, "The day of the month must be between 1 and 31.");

    private static string? NormaliseDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();

        return trimmed.Length > DescriptionMaxLength
            ? throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description))
            : trimmed;
    }
}
