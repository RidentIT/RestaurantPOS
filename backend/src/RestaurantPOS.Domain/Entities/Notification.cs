using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One alert delivered to one person.
/// </summary>
/// <remarks>
/// Stored per recipient rather than once with a list of readers: dismissing "the kitchen is out
/// of rice" should clear it from the cashier's bell without hiding it from the owner. A row per
/// person is a handful of extra rows a day and behaves the way people expect.
/// </remarks>
public sealed class Notification : BaseEntity
{
    public const int TitleMaxLength = 150;
    public const int BodyMaxLength = 500;
    public const int DedupeKeyMaxLength = 200;
    public const int LinkMaxLength = 200;

    // EF Core materialisation.
    private Notification()
    {
    }

    private Notification(
        Guid userId,
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string? body,
        string dedupeKey,
        string? link,
        DateTime raisedAtUtc)
    {
        UserId = userId;
        Type = type;
        Severity = severity;
        Title = title;
        Body = body;
        DedupeKey = dedupeKey;
        Link = link;
        RaisedAtUtc = raisedAtUtc;
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public NotificationSeverity Severity { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Body { get; private set; }

    /// <summary>
    /// Identifies the thing being reported — "the kitchen is low on rice" rather than "something
    /// is low". Two evaluations of the same standing condition produce the same key, which is how
    /// a low stock level notifies once instead of every minute until someone reorders.
    /// </summary>
    public string DedupeKey { get; private set; } = string.Empty;

    /// <summary>Where clicking it should go, so an alert is something you can act on.</summary>
    public string? Link { get; private set; }

    public DateTime RaisedAtUtc { get; private set; }

    public DateTime? ReadAtUtc { get; private set; }

    public bool IsRead => ReadAtUtc is not null;

    public static Notification Raise(
        Guid userId,
        NotificationType type,
        NotificationSeverity severity,
        string title,
        string? body,
        string dedupeKey,
        string? link,
        DateTime raisedAtUtc) =>
        new(userId, type, severity, Truncate(title, TitleMaxLength) ?? string.Empty,
            Truncate(body, BodyMaxLength), dedupeKey, link, raisedAtUtc);

    public void MarkRead(DateTime nowUtc) => ReadAtUtc ??= nowUtc;

    /// <summary>
    /// Titles and bodies are composed from live data — a raw material name, a supplier name — so
    /// an unusually long one is trimmed rather than allowed to fail the insert. Losing the tail
    /// of a sentence is better than losing the alert.
    /// </summary>
    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..(maxLength - 1)]}…";
    }
}
