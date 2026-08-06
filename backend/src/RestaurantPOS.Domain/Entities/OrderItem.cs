using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One line on a bill. Name and price are copied from the <see cref="MenuItem"/> at the moment it
/// was ordered rather than read through a relation, so re-pricing the menu tonight never rewrites
/// what a customer was charged last week — a printed receipt has to stay reproducible (POS-029).
/// </summary>
public sealed class OrderItem : BaseEntity
{
    public const int SpecialInstructionsMaxLength = 250;

    // EF Core materialisation.
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid menuItemId,
        string menuItemName,
        decimal unitPrice,
        int quantity,
        string? specialInstructions)
    {
        OrderId = orderId;
        MenuItemId = menuItemId;
        MenuItemName = menuItemName;
        UnitPrice = unitPrice >= 0
            ? unitPrice
            : throw new ArgumentOutOfRangeException(nameof(unitPrice), unitPrice, "Unit price cannot be negative.");
        Quantity = ValidateQuantity(quantity);
        SpecialInstructions = NormaliseInstructions(specialInstructions);
    }

    public Guid OrderId { get; private set; }

    public Guid MenuItemId { get; private set; }

    /// <summary>The dish name as it was when ordered.</summary>
    public string MenuItemName { get; private set; } = string.Empty;

    /// <summary>The price charged per unit, fixed at the time of ordering.</summary>
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public string? SpecialInstructions { get; private set; }

    public bool IsCancelled { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>What this line contributes to the bill. A cancelled line contributes nothing.</summary>
    public decimal LineTotal => IsCancelled ? 0m : UnitPrice * Quantity;

    /// <summary>Changes how many were ordered. Callers gate this behind an approval PIN (BR-POS-007).</summary>
    public void ChangeQuantity(int quantity)
    {
        EnsureNotCancelled();
        Quantity = ValidateQuantity(quantity);
    }

    /// <summary>Voids the line, keeping it on the order as a record of what was taken off.</summary>
    public void Cancel(DateTime nowUtc)
    {
        EnsureNotCancelled();
        IsCancelled = true;
        CancelledAtUtc = nowUtc;
    }

    private void EnsureNotCancelled()
    {
        if (IsCancelled)
        {
            throw new InvalidOperationException("A cancelled order item cannot be changed.");
        }
    }

    private static int ValidateQuantity(int quantity) =>
        quantity > 0
            ? quantity
            : throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than zero.");

    private static string? NormaliseInstructions(string? instructions)
    {
        if (string.IsNullOrWhiteSpace(instructions))
        {
            return null;
        }

        var trimmed = instructions.Trim();

        return trimmed.Length > SpecialInstructionsMaxLength
            ? throw new ArgumentException(
                $"Special instructions cannot exceed {SpecialInstructionsMaxLength} characters.", nameof(instructions))
            : trimmed;
    }
}
