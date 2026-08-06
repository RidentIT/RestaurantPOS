using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One KOT: a slip printed for the kitchen and a card on the kitchen display. An order
/// accumulates several of these — the confirmation ticket plus one per amendment (POS-014,
/// POS-020) — because the kitchen has already started on the earlier ones and needs to be told
/// what changed, not handed a fresh copy of the whole order.
/// </summary>
public sealed class KitchenTicket : BaseEntity
{
    private readonly List<KitchenTicketLine> _lines = [];

    // EF Core materialisation.
    private KitchenTicket()
    {
    }

    internal KitchenTicket(Guid orderId, int ticketNumber, KitchenTicketKind kind, DateTime nowUtc)
    {
        OrderId = orderId;
        TicketNumber = ticketNumber;
        Kind = kind;
        Status = KitchenTicketStatus.New;
        PrintedAtUtc = nowUtc;
    }

    public Guid OrderId { get; private set; }

    /// <summary>Position within its order, starting at 1 — the "KOT-2" on the printed slip.</summary>
    public int TicketNumber { get; private set; }

    public KitchenTicketKind Kind { get; private set; }

    public KitchenTicketStatus Status { get; private set; }

    public DateTime PrintedAtUtc { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }

    public DateTime? ReadyAtUtc { get; private set; }

    public DateTime? ServedAtUtc { get; private set; }

    /// <summary>How many times the slip has been sent to the printer, including the first.</summary>
    public int PrintCount { get; private set; } = 1;

    public IReadOnlyCollection<KitchenTicketLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// True when this ticket represents food to cook. Cancellation slips are notices — they are
    /// shown to the kitchen but must not drag a table's progress backwards.
    /// </summary>
    public bool IsWorkable => Kind != KitchenTicketKind.Cancellation;

    internal void AddLine(Guid orderItemId, string menuItemName, int quantity, string? specialInstructions, string? note) =>
        _lines.Add(new KitchenTicketLine(Id, orderItemId, menuItemName, quantity, specialInstructions, note));

    /// <summary>
    /// Moves the ticket on to <paramref name="status"/>. Forward-only: prep timings are read back
    /// as kitchen performance, so a ticket cannot be walked backwards to manufacture a better one.
    /// </summary>
    public void Advance(KitchenTicketStatus status, DateTime nowUtc)
    {
        if (status <= Status)
        {
            throw new InvalidOperationException(
                $"A kitchen ticket cannot move from {Status} back to {status}.");
        }

        Status = status;

        switch (status)
        {
            case KitchenTicketStatus.Preparing:
                StartedAtUtc = nowUtc;
                break;
            case KitchenTicketStatus.Ready:
                ReadyAtUtc = nowUtc;
                break;
            case KitchenTicketStatus.Served:
                ServedAtUtc = nowUtc;
                break;
            default:
                break;
        }
    }

    public void RecordReprint() => PrintCount++;
}
