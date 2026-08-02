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
}