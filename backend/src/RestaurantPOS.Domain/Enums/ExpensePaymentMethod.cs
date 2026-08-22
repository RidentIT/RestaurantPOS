namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// How an expense was settled (BR-EXP-011). Separate from the till's
/// <see cref="OrderPaymentMethod"/> and the supplier ledger's <see cref="PaymentMethod"/>: money
/// paid out for a gas cylinder accepts a different set of methods than a customer settling a bill.
/// </summary>
public enum ExpensePaymentMethod
{
    Cash = 1,
    Cheque = 2,
    Card = 3,
    BankTransfer = 4,
}
