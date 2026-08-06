namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// How a customer settles a bill (POS-022). Deliberately separate from the supplier-side
/// <see cref="PaymentMethod"/>: money going out to a supplier and money coming in at the till are
/// different transactions with different accepted methods, and conflating them would force one
/// enum to carry values that are meaningless on the other side.
/// </summary>
public enum OrderPaymentMethod
{
    Cash = 1,
    Card = 2,
    Qr = 3,
    BankTransfer = 4,
}
