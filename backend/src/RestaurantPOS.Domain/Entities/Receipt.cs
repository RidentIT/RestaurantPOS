using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// The record that a bill was settled and a slip printed (POS-030). It carries the receipt number
/// and print history only — the printed content is rebuilt from the order, which is frozen once
/// completed, so a reprint (POS-029) can never drift from the original.
/// </summary>
public sealed class Receipt : BaseEntity
{
    public const int NumberMaxLength = 30;

    // EF Core materialisation.
    private Receipt()
    {
    }

    internal Receipt(Guid orderId, string number, DateTime issuedAtUtc)
    {
        OrderId = orderId;
        Number = number;
        IssuedAtUtc = issuedAtUtc;
        LastPrintedAtUtc = issuedAtUtc;
    }

    public Guid OrderId { get; private set; }

    /// <summary>Customer-facing reference, e.g. "REC-20260809-001".</summary>
    public string Number { get; private set; } = string.Empty;

    public DateTime IssuedAtUtc { get; private set; }

    /// <summary>Times the slip has been printed, including the one handed over at payment.</summary>
    public int PrintCount { get; private set; } = 1;

    public DateTime LastPrintedAtUtc { get; private set; }

    public void RecordReprint(DateTime nowUtc)
    {
        PrintCount++;
        LastPrintedAtUtc = nowUtc;
    }
}
