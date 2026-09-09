using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by notification, preference and threshold use cases.</summary>
public static class NotificationErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Notification.NotFound", $"No notification was found with id '{id}'.");

    public static readonly Error UnknownType =
        Error.Validation("Notification.UnknownType", "That notification type is not in the catalog.");

    public static readonly Error NotYours =
        Error.Forbidden("Notification.NotYours", "That notification belongs to somebody else.");

    public static readonly Error NoThreshold =
        Error.Validation(
            "Notification.NoThreshold", "This notification has no threshold that can be configured.");

    public static readonly Error NegativeThreshold =
        Error.Validation("Notification.NegativeThreshold", "A threshold cannot be negative.");
}
