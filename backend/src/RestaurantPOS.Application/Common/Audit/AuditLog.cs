using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Audit;

/// <summary>Thin helper so command handlers can write an <see cref="AuditLogEntry"/> in one line.</summary>
public static class AuditLog
{
    public static void Record(
        IAppDbContext db,
        string entityType,
        Guid entityId,
        string action,
        Guid performedByUserId,
        string performedByName,
        DateTime nowUtc,
        string summary,
        string? detailsJson = null)
    {
        ArgumentNullException.ThrowIfNull(db);

        db.AuditLogEntries.Add(AuditLogEntry.Record(
            entityType, entityId, action, performedByUserId, performedByName, nowUtc, summary, detailsJson));
    }
}