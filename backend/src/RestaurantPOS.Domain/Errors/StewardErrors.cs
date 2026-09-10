using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by steward administration and by crediting an order to one.</summary>
public static class StewardErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Steward.NotFound", $"No steward was found with id '{id}'.");

    public static readonly Error NameTaken =
        Error.Conflict("Steward.NameTaken", "A steward with that name already exists.");

    public static readonly Error Inactive =
        Error.Validation("Steward.Inactive", "That steward is no longer active and cannot be assigned to an order.");

    public static readonly Error NotOnTableOrder =
        Error.Validation("Steward.NotOnTableOrder", "A takeaway order cannot be assigned a steward.");
}
