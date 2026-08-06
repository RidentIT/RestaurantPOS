using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// An order placed with a supplier for one or more raw materials. Lines are editable only while
/// the order is still a <see cref="PurchaseOrderStatus.Draft"/> — once submitted, it represents
/// a real commitment sent to the supplier and its lines are frozen.
/// </summary>
public sealed class PurchaseOrder : BaseEntity
{
    public const int NotesMaxLength = 500;

    private readonly List<PurchaseOrderLine> _lines = [];

    // EF Core materialisation.
    private PurchaseOrder()
    {
    }

    private PurchaseOrder(
        Guid supplierId,
        Guid createdByUserId,
        IEnumerable<(Guid RawMaterialId, decimal Quantity, decimal UnitPrice)> lines,
        DateTime? expectedDeliveryDate,
        string? notes)
    {
        SupplierId = supplierId;
        CreatedByUserId = createdByUserId;
        Status = PurchaseOrderStatus.Draft;
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = NormaliseNotes(notes);
        SetLines(lines);
    }

    public Guid SupplierId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public DateTime? ExpectedDeliveryDate { get; private set; }

    /// <summary>When the order left Draft. Null until then; used to measure delivery time.</summary>
    public DateTime? SubmittedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public decimal TotalAmount => _lines.Sum(l => l.LineTotal);

    public static PurchaseOrder Create(
        Guid supplierId,
        Guid createdByUserId,
        IEnumerable<(Guid RawMaterialId, decimal Quantity, decimal UnitPrice)> lines,
        DateTime? expectedDeliveryDate = null,
        string? notes = null) =>
        new(supplierId, createdByUserId, lines, expectedDeliveryDate, notes);

    /// <summary>Replaces every line wholesale. Only while still a Draft.</summary>
    public void ReplaceLines(IEnumerable<(Guid RawMaterialId, decimal Quantity, decimal UnitPrice)> lines)
    {
        EnsureDraft();
        SetLines(lines);
    }

    public void UpdateDetails(DateTime? expectedDeliveryDate, string? notes)
    {
        EnsureDraft();
        ExpectedDeliveryDate = expectedDeliveryDate;
        Notes = NormaliseNotes(notes);
    }

    /// <summary>Sends the order to the supplier. No further line edits after this point.</summary>
    public void Submit(DateTime nowUtc)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft purchase order can be submitted.");
        }

        Status = PurchaseOrderStatus.Submitted;
        SubmittedAtUtc = nowUtc;
    }

    /// <summary>Records that the supplier has agreed to fulfil the order as sent.</summary>
    public void Confirm()
    {
        if (Status != PurchaseOrderStatus.Submitted)
        {
            throw new InvalidOperationException("Only a submitted purchase order can be confirmed.");
        }

        Status = PurchaseOrderStatus.Confirmed;
    }

    /// <summary>
    /// Marks the order delivered. Called when a Goods Received Note is recorded against it;
    /// idempotent because a single order can be received across more than one GRN.
    /// </summary>
    public void MarkDelivered()
    {
        if (Status is PurchaseOrderStatus.Delivered)
        {
            return;
        }

        if (Status is PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled purchase order cannot be marked delivered.");
        }

        Status = PurchaseOrderStatus.Delivered;
    }

    public void Cancel()
    {
        if (Status is PurchaseOrderStatus.Delivered or PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("A delivered or already-cancelled purchase order cannot be cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft purchase order can be edited.");
        }
    }

    private void SetLines(IEnumerable<(Guid RawMaterialId, decimal Quantity, decimal UnitPrice)> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var materialised = lines.ToList();

        if (materialised.Count == 0)
        {
            throw new ArgumentException("A purchase order must contain at least one line.", nameof(lines));
        }

        if (materialised.Select(l => l.RawMaterialId).Distinct().Count() != materialised.Count)
        {
            throw new ArgumentException("A raw material cannot appear more than once on the same purchase order.", nameof(lines));
        }

        _lines.Clear();
        _lines.AddRange(materialised.Select(l => new PurchaseOrderLine(Id, l.RawMaterialId, l.Quantity, l.UnitPrice)));
    }

    private static string? NormaliseNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();

        return trimmed.Length > NotesMaxLength
            ? throw new ArgumentException($"Notes cannot exceed {NotesMaxLength} characters.", nameof(notes))
            : trimmed;
    }
}