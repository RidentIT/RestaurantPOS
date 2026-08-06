using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A record of one change to one entity, for screens that need to show "recent changes" or
/// prove what a deleted record used to contain (BR-REC-007). Deliberately generic — keyed by
/// <see cref="EntityType"/> and <see cref="EntityId"/> — so any module can write to it, though
/// only Recipe Management does so today (REC-010).
/// </summary>
public sealed class AuditLogEntry : BaseEntity
{
    public const int EntityTypeMaxLength = 100;
    public const int ActionMaxLength = 50;
    public const int SummaryMaxLength = 500;

    // EF Core materialisation.
    private AuditLogEntry()
    {
    }

    private AuditLogEntry(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string performedByName,
        DateTime occurredAtUtc,
        string summary,
        string? detailsJson)
    {
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        PerformedByUserId = performedByUserId;
        PerformedByName = performedByName;
        OccurredAtUtc = occurredAtUtc;
        Summary = summary;
        DetailsJson = detailsJson;
    }

    /// <summary>The kind of entity changed, e.g. <c>"Recipe"</c>.</summary>
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    /// <summary>E.g. <c>"Created"</c>, <c>"Updated"</c>, <c>"Deleted"</c>, <c>"Enabled"</c>, <c>"Disabled"</c>.</summary>
    public string Action { get; private set; } = string.Empty;

    public Guid PerformedByUserId { get; private set; }

    /// <summary>Denormalised so history displays without joining back to Users.</summary>
    public string PerformedByName { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>One-line human-readable description of what changed.</summary>
    public string Summary { get; private set; } = string.Empty;

    /// <summary>Optional JSON snapshot of the entity's state, used to recover a deleted record's content.</summary>
    public string? DetailsJson { get; private set; }

    public static AuditLogEntry Record(
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string performedByName,
        DateTime occurredAtUtc,
        string summary,
        string? detailsJson = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return new AuditLogEntry(
            Truncate(entityType, EntityTypeMaxLength),
            entityId,
            Truncate(action, ActionMaxLength),
            performedByUserId,
            performedByName,
            occurredAtUtc,
            Truncate(summary, SummaryMaxLength),
            detailsJson);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}