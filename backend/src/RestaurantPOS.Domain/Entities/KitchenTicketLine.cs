using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One line as it was printed on a kitchen ticket. This is a snapshot, not a live view of the
/// order: a reprint has to show what the kitchen was actually handed, including the quantity that
/// was correct at print time even though it has since been amended.
/// </summary>
public sealed class KitchenTicketLine : BaseEntity
{
    public const int NoteMaxLength = 120;

    // EF Core materialisation.
    private KitchenTicketLine()
    {
    }

    internal KitchenTicketLine(
        Guid kitchenTicketId,
        Guid orderItemId,
        string menuItemName,
        int quantity,
        string? specialInstructions,
        string? note)
    {
        KitchenTicketId = kitchenTicketId;
        OrderItemId = orderItemId;
        MenuItemName = menuItemName;
        Quantity = quantity;
        SpecialInstructions = specialInstructions;
        Note = note;
    }

    public Guid KitchenTicketId { get; private set; }

    /// <summary>The bill line this was printed for, so the display can group amendments with it.</summary>
    public Guid OrderItemId { get; private set; }

    public string MenuItemName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public string? SpecialInstructions { get; private set; }

    /// <summary>Why this line is on an amendment ticket, e.g. "Was 1" or "CANCELLED".</summary>
    public string? Note { get; private set; }
}
