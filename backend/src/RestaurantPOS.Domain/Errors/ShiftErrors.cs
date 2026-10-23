using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by checking in and out, and by taking orders without a shift.</summary>
public static class ShiftErrors
{
    public static readonly Error NotCheckedIn =
        Error.Validation("Shift.NotCheckedIn", "Check in to start your shift before taking orders.");

    public static readonly Error AlreadyCheckedIn =
        Error.Conflict("Shift.AlreadyCheckedIn", "You already have a shift open.");

    public static readonly Error NoOpenShift =
        Error.Validation("Shift.NoOpenShift", "You don't have a shift open to check out of.");

    public static readonly Error ClosingCashRequired =
        Error.Validation("Shift.ClosingCashRequired", "Enter the cash you counted in the drawer to check out.");

    public static Error HasOpenOrders(int count) =>
        Error.Conflict(
            "Shift.HasOpenOrders",
            count == 1
                ? "You still have 1 unsettled order. Pay or cancel it before checking out."
                : $"You still have {count} unsettled orders. Pay or cancel them before checking out.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Shift.NotFound", $"No shift was found with id '{id}'.");

    public static readonly Error NotYours =
        Error.Forbidden("Shift.NotYours", "You can only view your own shifts.");

    public static readonly Error AlreadyClosed =
        Error.Conflict("Shift.AlreadyClosed", "That shift has already been closed.");
}
