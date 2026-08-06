using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// One step in an expense's approval history (EXP-014).
/// </summary>
/// <remarks>
/// Append-only. The expense itself carries who approved it and when, but that is only the last
/// word — this keeps the whole conversation, so an expense rejected on Monday and approved on
/// Wednesday still shows why it was turned down the first time.
/// </remarks>
public sealed class ExpenseApprovalEntry : BaseEntity
{
    public const int CommentsMaxLength = 500;

    // EF Core materialisation.
    private ExpenseApprovalEntry()
    {
    }

    internal ExpenseApprovalEntry(
        Guid expenseId,
        ExpenseStatus fromStatus,
        ExpenseStatus toStatus,
        Guid actedByUserId,
        DateTime actedAtUtc,
        string? comments)
    {
        ExpenseId = expenseId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActedByUserId = actedByUserId;
        ActedAtUtc = actedAtUtc;
        Comments = comments;
    }

    public Guid ExpenseId { get; private set; }

    public ExpenseStatus FromStatus { get; private set; }

    public ExpenseStatus ToStatus { get; private set; }

    public Guid ActedByUserId { get; private set; }

    public DateTime ActedAtUtc { get; private set; }

    public string? Comments { get; private set; }
}
