using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Expenses.Dtos;

public sealed record ExpenseCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    decimal? MonthlyBudget,
    Guid? ParentCategoryId,
    string? ParentCategoryName,
    bool IsSystem,
    bool IsActive,
    /// <summary>Expenses recorded against it, so the UI can explain why it cannot be deleted.</summary>
    int ExpenseCount);

public sealed record ExpenseDto(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Description,
    ExpenseStatus Status,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid,
    Guid RecordedByUserId,
    string RecordedByName,
    DateTime CreatedAtUtc,
    Guid? ApprovedByUserId,
    string? ApprovedByName,
    DateTime? ApprovedAtUtc,
    string? ApprovalComments,
    bool IsRecurring,
    bool IsEditable,
    IReadOnlyCollection<ExpenseAttachmentDto> Attachments,
    IReadOnlyCollection<ExpenseApprovalEntryDto> ApprovalTrail);

public sealed record ExpenseAttachmentDto(
    Guid Id, string FileName, string ContentType, long SizeBytes, DateTime UploadedAtUtc, string UploadedByName);

public sealed record ExpenseApprovalEntryDto(
    ExpenseStatus FromStatus,
    ExpenseStatus ToStatus,
    Guid ActedByUserId,
    string ActedByName,
    DateTime ActedAtUtc,
    string? Comments);

/// <summary>An expense's header for the list screen, without its trail or attachments.</summary>
public sealed record ExpenseSummaryDto(
    Guid Id,
    string ExpenseNumber,
    DateOnly ExpenseDate,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Description,
    ExpenseStatus Status,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    bool IsPaid,
    bool IsRecurring,
    int AttachmentCount,
    string RecordedByName);

public sealed record RecurringExpenseDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    int DayOfMonth,
    bool IsActive,
    int? LastGeneratedYear,
    int? LastGeneratedMonth);
