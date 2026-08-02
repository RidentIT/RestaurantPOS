using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by user administration use cases.</summary>
public static class UserErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("User.NotFound", $"No user was found with id '{id}'.");

    public static readonly Error UsernameTaken =
        Error.Conflict("User.UsernameTaken", "That username is already in use.");

    public static readonly Error CannotDeactivateSelf =
        Error.Conflict("User.CannotDeactivateSelf", "You cannot deactivate your own account.");

    public static readonly Error CannotDemoteSelf =
        Error.Conflict("User.CannotDemoteSelf", "You cannot change your own role.");

    public static readonly Error CannotModifySystemAdmin =
        Error.Conflict(
            "User.CannotModifySystemAdmin",
            "The built-in administrator account cannot be deactivated or have its role changed.");

    public static readonly Error LastAdmin =
        Error.Conflict(
            "User.LastAdmin",
            "At least one active administrator must remain. Promote another user first.");

    public static readonly Error ModulesNotApplicableToAdmin =
        Error.Validation(
            "User.ModulesNotApplicableToAdmin",
            "Administrators already have access to every module, so module grants cannot be set for them.");

    public static readonly Error UnknownModule =
        Error.Validation("User.UnknownModule", "One or more of the selected modules is not recognised.");
}