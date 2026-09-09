using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Orders.Dtos;

/// <summary>
/// Everything that goes on a printed slip, assembled server-side.
/// </summary>
/// <remarks>
/// The API returns the content of a document rather than rendering it, because the printer lives
/// on the cashier's machine: the desktop shell lays this out as an 80mm page and sends it to the
/// Windows printer driver. Composing the numbers here keeps a receipt's arithmetic in one place
/// instead of trusting the till to add the bill up a second time.
/// </remarks>
public sealed record KotDocumentDto(
    Guid TicketId,
    string RestaurantName,
    int? OrderNumber,
    /// <summary>Null for a takeaway order.</summary>
    string? TableNumber,
    int TicketNumber,
    KitchenTicketKind Kind,
    string CashierName,
    DateTime PrintedAtUtc,
    int PrintCount,
    /// <summary>The Windows printer this slip should go to. Null uses the till's default printer.</summary>
    string? PrinterName,
    IReadOnlyCollection<KotDocumentLineDto> Lines);

public sealed record KotDocumentLineDto(
    string MenuItemName, int Quantity, string? SpecialInstructions, string? Note);

/// <summary>A customer receipt as it should appear on 80mm paper (POS-027).</summary>
public sealed record ReceiptDocumentDto(
    string ReceiptNumber,
    string RestaurantName,
    string AddressLine1,
    string? AddressLine2,
    string? City,
    string? Phone,
    /// <summary>Null when the restaurant hasn't set one — plenty of small operations aren't VAT-registered at all.</summary>
    string? VatRegistrationNumber,
    int? OrderNumber,
    /// <summary>Null for a takeaway order.</summary>
    string? TableNumber,
    string CashierName,
    DateTime IssuedAtUtc,
    int PrintCount,
    /// <summary>The Windows printer this receipt should go to. Null uses the till's default printer.</summary>
    string? PrinterName,
    IReadOnlyCollection<ReceiptLineDto> Lines,
    decimal Subtotal,
    decimal DiscountAmount,
    /// <summary>Zero unless the restaurant has configured a service charge rate (BR-POS-012).</summary>
    decimal ServiceChargeAmount,
    /// <summary>Zero unless the restaurant has configured a tax rate (BR-POS-011).</summary>
    decimal TaxAmount,
    decimal Total,
    decimal ChangeGiven,
    IReadOnlyCollection<OrderPaymentDto> Payments,
    /// <summary>Encoded on the slip as a QR code so a bill can be looked up from paper (POS-028).</summary>
    string QrPayload,
    string FooterMessage);

public sealed record ReceiptLineDto(string MenuItemName, int Quantity, decimal UnitPrice, decimal LineTotal);
