using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by authentication and approval-PIN use cases.</summary>
public static class AuthErrors
{
    /// <summary>
    /// Deliberately identical for an unknown username and a wrong password so the API
    /// does not reveal which usernames exist.
    /// </summary>
    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "The username or password is incorrect.");

    public static readonly Error AccountDeactivated =
        Error.Forbidden("Auth.AccountDeactivated", "This account has been deactivated. Contact an administrator.");

    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Your session has expired. Please sign in again.");

    public static readonly Error PasswordMismatch =
        Error.Validation("Auth.PasswordMismatch", "The current password is incorrect.");

    public static readonly Error PasswordReused =
        Error.Validation("Auth.PasswordReused", "The new password must be different from the current password.");

    public static readonly Error PinNotSet =
        Error.NotFound("Auth.PinNotSet", "No approval PIN has been set for this account.");

    public static readonly Error InvalidPin =
        Error.Validation("Auth.InvalidPin", "That approval PIN is not valid.");

    public static readonly Error PinRequiresAdmin =
        Error.Validation("Auth.PinRequiresAdmin", "Only administrators can hold an approval PIN.");

    public static readonly Error NoAdminPinConfigured =
        Error.Conflict(
            "Auth.NoAdminPinConfigured",
            "No administrator has configured an approval PIN yet.");

    /// <summary>
    /// A wrong PIN, told to the person at the till how many tries are left. Counting down out
    /// loud is deliberate: the cashier is usually mistyping a PIN they were told correctly, and
    /// a silent lockout mid-service reads as the till breaking.
    /// </summary>
    public static Error InvalidPinWithAttemptsLeft(int attemptsRemaining) =>
        Error.Validation(
            "Auth.InvalidPin",
            $"That approval PIN is not valid. {attemptsRemaining} attempt{(attemptsRemaining == 1 ? "" : "s")} remaining.");

    /// <summary>
    /// Too many wrong PINs from one terminal. This pauses PIN entry on that terminal only —
    /// administrator accounts are never disabled, because locking out the sole administrator
    /// mid-service would leave the restaurant unable to approve anything at all.
    /// </summary>
    public static Error PinAttemptsExhausted(TimeSpan retryAfter) =>
        Error.Forbidden(
            "Auth.PinAttemptsExhausted",
            $"Too many incorrect PIN attempts. Try again in {retryAfter.Minutes}:{retryAfter.Seconds:00}.");
}