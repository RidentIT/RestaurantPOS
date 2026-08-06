using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One tender against a bill. A bill can carry several of these so a table can settle part in
/// cash and the rest on a card (POS-023); together they must come to exactly the bill total
/// (BR-POS-013).
/// </summary>
public sealed class OrderPayment : BaseEntity
{
    public const int ReferenceMaxLength = 100;

    // EF Core materialisation.
    private OrderPayment()
    {
    }

    internal OrderPayment(
        Guid orderId,
        OrderPaymentMethod method,
        decimal amount,
        decimal? tenderedAmount,
        string? reference)
    {
        OrderId = orderId;
        Method = method;
        Amount = amount > 0
            ? amount
            : throw new ArgumentOutOfRangeException(nameof(amount), amount, "A payment must be greater than zero.");
        TenderedAmount = tenderedAmount;
        Reference = reference;
    }

    public Guid OrderId { get; private set; }

    public OrderPaymentMethod Method { get; private set; }

    /// <summary>What this tender contributes to the bill.</summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Cash handed over, when it exceeds <see cref="Amount"/>. Kept so the receipt can show what
    /// was given and what came back (POS-024); null for every non-cash method.
    /// </summary>
    public decimal? TenderedAmount { get; private set; }

    /// <summary>Card approval code, transfer reference, or similar.</summary>
    public string? Reference { get; private set; }

    /// <summary>Change handed back, or zero when the exact amount was tendered.</summary>
    public decimal ChangeGiven => TenderedAmount is null ? 0m : Math.Max(0m, TenderedAmount.Value - Amount);
}
