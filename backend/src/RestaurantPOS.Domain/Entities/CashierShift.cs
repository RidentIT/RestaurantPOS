using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One cashier's stint at the till, from checking in to checking out. Every order is stamped with
/// the shift it was opened in, so a shift's sales and cash can be totalled exactly — independent of
/// what the calendar says, which matters in a restaurant that never closes.
/// </summary>
/// <remarks>
/// Kept forever, never deleted: how far back a cashier can look is a display rule, not a retention
/// rule, and the owner can always see the full history.
/// </remarks>
public sealed class CashierShift : BaseEntity
{
    public const int NoteMaxLength = 300;

    // EF Core materialisation.
    private CashierShift()
    {
    }

    private CashierShift(Guid cashierUserId, decimal openingCash, DateTime nowUtc)
    {
        CashierUserId = cashierUserId;
        OpeningCash = ValidateCash(openingCash, nameof(openingCash));
        CheckedInAtUtc = nowUtc;
    }

    public Guid CashierUserId { get; private set; }

    public DateTime CheckedInAtUtc { get; private set; }

    /// <summary>Cash in the drawer when the shift began — the float handed over.</summary>
    public decimal OpeningCash { get; private set; }

    /// <summary>Null while the shift is still open.</summary>
    public DateTime? CheckedOutAtUtc { get; private set; }

    /// <summary>
    /// What the cashier physically counted at the end. Null when the shift was closed by the owner
    /// without a count, e.g. after a cashier forgot to check out.
    /// </summary>
    public decimal? ClosingCash { get; private set; }

    /// <summary>Opening cash plus the cash the shift took, frozen at the moment it was closed.</summary>
    public decimal? ExpectedCash { get; private set; }

    /// <summary>Counted less expected: negative is a shortage, positive an overage.</summary>
    public decimal? Variance => ClosingCash is null || ExpectedCash is null ? null : ClosingCash - ExpectedCash;

    public string? Note { get; private set; }

    /// <summary>The user who closed the shift — the cashier themselves, or the owner forcing it shut.</summary>
    public Guid? ClosedByUserId { get; private set; }

    public bool IsOpen => CheckedOutAtUtc is null;

    public static CashierShift Start(Guid cashierUserId, decimal openingCash, DateTime nowUtc) =>
        new(cashierUserId, openingCash, nowUtc);

    public void Close(decimal? closingCash, decimal expectedCash, string? note, Guid closedByUserId, DateTime nowUtc)
    {
        if (!IsOpen)
        {
            throw new InvalidOperationException("This shift has already been closed.");
        }

        ClosingCash = closingCash is null ? null : ValidateCash(closingCash.Value, nameof(closingCash));
        ExpectedCash = expectedCash;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, NoteMaxLength)];
        ClosedByUserId = closedByUserId;
        CheckedOutAtUtc = nowUtc;
    }

    private static decimal ValidateCash(decimal value, string paramName) =>
        value >= 0
            ? Math.Round(value, 2, MidpointRounding.AwayFromZero)
            : throw new ArgumentOutOfRangeException(paramName, value, "A cash amount cannot be negative.");
}
