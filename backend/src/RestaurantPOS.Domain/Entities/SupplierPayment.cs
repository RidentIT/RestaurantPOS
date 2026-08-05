using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A payment made toward a Purchase Order. A PO's paid/pending status is derived by comparing
/// the sum of its payments to its total, rather than stored as its own field — so it can never
/// go stale relative to the payments actually recorded.
/// </summary>
public sealed class SupplierPayment : BaseEntity
{
    public const int InvoiceReferenceMaxLength = 100;
    public const int NotesMaxLength = 500;

    // EF Core materialisation.
    private SupplierPayment()
    {
    }

    private SupplierPayment(
        Guid purchaseOrderId,
        decimal amount,
        DateTime paymentDateUtc,
        PaymentMethod method,
        string? invoiceReference,
        Guid recordedByUserId,
        string? notes)
    {
        PurchaseOrderId = purchaseOrderId;
        Amount = amount > 0
            ? amount
            : throw new ArgumentOutOfRangeException(nameof(amount), amount, "Payment amount must be greater than zero.");
        PaymentDateUtc = paymentDateUtc;
        Method = method;
        InvoiceReference = NormaliseOptional(invoiceReference, InvoiceReferenceMaxLength, nameof(invoiceReference));
        RecordedByUserId = recordedByUserId;
        Notes = NormaliseOptional(notes, NotesMaxLength, nameof(notes));
    }

    public Guid PurchaseOrderId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime PaymentDateUtc { get; private set; }

    public PaymentMethod Method { get; private set; }

    public string? InvoiceReference { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public string? Notes { get; private set; }

    public static SupplierPayment Create(
        Guid purchaseOrderId,
        decimal amount,
        DateTime paymentDateUtc,
        PaymentMethod method,
        Guid recordedByUserId,
        string? invoiceReference = null,
        string? notes = null) =>
        new(purchaseOrderId, amount, paymentDateUtc, method, invoiceReference, recordedByUserId, notes);

    private static string? NormaliseOptional(string? value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? throw new ArgumentException($"'{paramName}' cannot exceed {maxLength} characters.", paramName)
            : trimmed;
    }
}