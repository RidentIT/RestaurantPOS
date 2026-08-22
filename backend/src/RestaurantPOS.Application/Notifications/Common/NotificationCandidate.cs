using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Notifications.Common;

/// <summary>
/// Something worth telling somebody about, before it is decided who that somebody is.
/// </summary>
/// <remarks>
/// Rules produce these; the dispatcher works out who is eligible, who has switched it off and
/// whether it has been said recently enough to stay quiet. Keeping the two apart means a rule
/// never has to know about users, permissions or de-duplication — it just answers "what is true
/// right now?".
/// </remarks>
/// <param name="DedupeKey">
/// Identifies the specific thing being reported, e.g. <c>LowStock:Kitchen:{rawMaterialId}</c>.
/// The same standing condition must produce the same key every time it is evaluated.
/// </param>
public sealed record NotificationCandidate(
    NotificationType Type,
    string DedupeKey,
    string Title,
    string? Body,
    string? Link);
