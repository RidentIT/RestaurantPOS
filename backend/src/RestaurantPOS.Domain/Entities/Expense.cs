using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// Money the restaurant paid out — a gas refill, the CEB bill, staff lunch.
/// </summary>
/// <remarks>
/// Freezes on approval (BR-EXP-007, EXP-036). An approved expense has been counted into a day's
/// profit and quite possibly reported to the owner, so letting the figure move afterwards would
/// silently rewrite history that somebody has already acted on. Correcting an approved expense
/// means rejecting it and recording the right one, which leaves both visible.
/// </remarks>
public sealed class Expense : BaseEntity
{
    public const int DescriptionMaxLength = 500;
    public const int ReferenceMaxLength = 100;

    private readonly List<ExpenseApprovalEntry> _approvalTrail = [];
    private readonly List<ExpenseAttachment> _attachments = [];

    // EF Core materialisation.
    private Expense()
    {
    }

    private Expense(
        int number,
        DateOnly expenseDate,
        Guid categoryId,
        decimal amount,
        string? description,
        ExpensePaymentMethod paymentMethod,
        string? paymentReference,
        DateOnly? paymentDate,
        bool isPaid,
        Guid recordedByUserId,
        Guid? recurringExpenseId)
    {
        Number = number;
        Year = expenseDate.Year;
        ExpenseDate = expenseDate;
        CategoryId = categoryId;
        Amount = ValidateAmount(amount);
        Description = NormaliseDescription(description);
        PaymentMethod = paymentMethod;
        PaymentReference = ValidateReference(paymentMethod, paymentReference);
        PaymentDate = paymentDate;
        IsPaid = isPaid;
        RecordedByUserId = recordedByUserId;
        RecurringExpenseId = recurringExpenseId;
        Status = ExpenseStatus.Draft;
    }

    /// <summary>Sequence within <see cref="Year"/>, part of the reference staff quote (EXP-009).</summary>
    public int Number { get; private set; }

    public int Year { get; private set; }

    /// <summary>The customer-facing reference, e.g. "EXP-001-2026".</summary>
    public string ExpenseNumber => $"EXP-{Number:000}-{Year}";

    /// <summary>The day the money was spent, which is the day it counts against (EXP-002).</summary>
    public DateOnly ExpenseDate { get; private set; }

    public Guid CategoryId { get; private set; }

    public decimal Amount { get; private set; }

    public string? Description { get; private set; }

    public ExpenseStatus Status { get; private set; }

    public ExpensePaymentMethod PaymentMethod { get; private set; }

    /// <summary>Cheque number, card slip or transfer reference. Required for everything but cash.</summary>
    public string? PaymentReference { get; private set; }

    /// <summary>When the money actually leaves, which can be later than the expense date.</summary>
    public DateOnly? PaymentDate { get; private set; }

    /// <summary>Whether the money has gone out yet (EXP-034).</summary>
    public bool IsPaid { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    /// <summary>The standing instruction that generated this, when it was not keyed in by hand.</summary>
    public Guid? RecurringExpenseId { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public string? ApprovalComments { get; private set; }

    public IReadOnlyCollection<ExpenseApprovalEntry> ApprovalTrail => _approvalTrail.AsReadOnly();

    public IReadOnlyCollection<ExpenseAttachment> Attachments => _attachments.AsReadOnly();

    /// <summary>True while the expense can still be changed — before anybody has ruled on it.</summary>
    public bool IsEditable => Status is ExpenseStatus.Draft or ExpenseStatus.Pending;

    /// <summary>True when this expense counts towards reports and profit (BR-EXP-005, BR-EXP-010).</summary>
    public bool CountsTowardsReports => Status == ExpenseStatus.Approved;

    public static Expense Create(
        int number,
        DateOnly expenseDate,
        Guid categoryId,
        decimal amount,
        string? description,
        ExpensePaymentMethod paymentMethod,
        string? paymentReference,
        DateOnly? paymentDate,
        bool isPaid,
        Guid recordedByUserId,
        Guid? recurringExpenseId = null) =>
        new(number, expenseDate, categoryId, amount, description, paymentMethod,
            paymentReference, paymentDate, isPaid, recordedByUserId, recurringExpenseId);

    public void UpdateDetails(
        DateOnly expenseDate,
        Guid categoryId,
        decimal amount,
        string? description,
        ExpensePaymentMethod paymentMethod,
        string? paymentReference,
        DateOnly? paymentDate,
        bool isPaid)
    {
        EnsureEditable();

        ExpenseDate = expenseDate;
        Year = expenseDate.Year;
        CategoryId = categoryId;
        Amount = ValidateAmount(amount);
        Description = NormaliseDescription(description);
        PaymentMethod = paymentMethod;
        PaymentReference = ValidateReference(paymentMethod, paymentReference);
        PaymentDate = paymentDate;
        IsPaid = isPaid;
    }

    /// <summary>Puts a draft forward for a manager to rule on.</summary>
    public void Submit(Guid submittedByUserId, DateTime nowUtc, string? comments = null)
    {
        if (Status != ExpenseStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft expense can be submitted for approval.");
        }

        RecordTransition(ExpenseStatus.Pending, submittedByUserId, nowUtc, comments);
    }

    /// <summary>Signs the expense off, freezing it and letting it count towards reports.</summary>
    public void Approve(Guid approvedByUserId, DateTime nowUtc, string? comments = null)
    {
        EnsureAwaitingDecision();

        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = nowUtc;
        ApprovalComments = NormaliseComments(comments);

        RecordTransition(ExpenseStatus.Approved, approvedByUserId, nowUtc, comments);
    }

    /// <summary>Turns the expense down. It stays on record rather than being deleted (BR-EXP-006).</summary>
    public void Reject(Guid rejectedByUserId, DateTime nowUtc, string? comments = null)
    {
        EnsureAwaitingDecision();

        ApprovedByUserId = rejectedByUserId;
        ApprovedAtUtc = nowUtc;
        ApprovalComments = NormaliseComments(comments);

        RecordTransition(ExpenseStatus.Rejected, rejectedByUserId, nowUtc, comments);
    }

    /// <summary>Records that the money has, or has not, actually gone out (EXP-034).</summary>
    public void SetPaid(bool isPaid, DateOnly? paymentDate)
    {
        IsPaid = isPaid;
        PaymentDate = isPaid ? paymentDate ?? PaymentDate : null;
    }

    /// <summary>Files a receipt against the expense, while it can still be changed.</summary>
    public ExpenseAttachment AddAttachment(
        string fileName, string storedPath, string contentType, long sizeBytes, Guid uploadedByUserId, DateTime nowUtc)
    {
        EnsureEditable();

        var attachment = new ExpenseAttachment(
            Id, fileName, storedPath, contentType, sizeBytes, uploadedByUserId, nowUtc);

        _attachments.Add(attachment);

        return attachment;
    }

    /// <summary>Takes a filed receipt back off the expense.</summary>
    public void RemoveAttachment(ExpenseAttachment attachment)
    {
        EnsureEditable();
        _attachments.Remove(attachment);
    }

    private void RecordTransition(ExpenseStatus toStatus, Guid actedByUserId, DateTime nowUtc, string? comments)
    {
        _approvalTrail.Add(new ExpenseApprovalEntry(
            Id, Status, toStatus, actedByUserId, nowUtc, NormaliseComments(comments)));

        Status = toStatus;
    }

    private void EnsureAwaitingDecision()
    {
        if (Status is not (ExpenseStatus.Draft or ExpenseStatus.Pending))
        {
            throw new InvalidOperationException("Only a draft or pending expense can be approved or rejected.");
        }
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
        {
            throw new InvalidOperationException($"An expense that is {Status} can no longer be changed.");
        }
    }

    private static decimal ValidateAmount(decimal amount) =>
        amount > 0
            ? amount
            : throw new ArgumentOutOfRangeException(nameof(amount), amount, "An expense amount must be greater than zero.");

    /// <summary>
    /// Cash leaves no paper trail of its own, so it needs no reference; every other method
    /// produces a cheque number, slip or transfer id, and an expense without one cannot be tied
    /// back to the bank statement.
    /// </summary>
    private static string? ValidateReference(ExpensePaymentMethod method, string? reference)
    {
        var trimmed = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();

        if (trimmed is not null && trimmed.Length > ReferenceMaxLength)
        {
            throw new ArgumentException(
                $"Reference cannot exceed {ReferenceMaxLength} characters.", nameof(reference));
        }

        return method != ExpensePaymentMethod.Cash && trimmed is null
            ? throw new ArgumentException(
                "A payment reference is required for cheque, card and bank transfer payments.", nameof(reference))
            : trimmed;
    }

    private static string? NormaliseDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();

        return trimmed.Length > DescriptionMaxLength
            ? throw new ArgumentException(
                $"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description))
            : trimmed;
    }

    private static string? NormaliseComments(string? comments)
    {
        if (string.IsNullOrWhiteSpace(comments))
        {
            return null;
        }

        var trimmed = comments.Trim();

        return trimmed.Length > ExpenseApprovalEntry.CommentsMaxLength
            ? throw new ArgumentException(
                $"Comments cannot exceed {ExpenseApprovalEntry.CommentsMaxLength} characters.", nameof(comments))
            : trimmed;
    }
}
