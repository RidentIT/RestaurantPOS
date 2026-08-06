using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projections from the expense aggregate to the shapes the screens read.</summary>
public static class ExpenseMappings
{
    public static ExpenseCategoryDto ToDto(
        this ExpenseCategory category, string? parentName = null, int expenseCount = 0)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new ExpenseCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.MonthlyBudget,
            category.ParentCategoryId,
            parentName,
            category.IsSystem,
            category.IsActive,
            expenseCount);
    }

    public static ExpenseDto ToDto(
        this Expense expense,
        string categoryName,
        string recordedByName,
        IReadOnlyDictionary<Guid, string> userNames)
    {
        ArgumentNullException.ThrowIfNull(expense);
        ArgumentNullException.ThrowIfNull(userNames);

        return new ExpenseDto(
            expense.Id,
            expense.ExpenseNumber,
            expense.ExpenseDate,
            expense.CategoryId,
            categoryName,
            expense.Amount,
            expense.Description,
            expense.Status,
            expense.PaymentMethod,
            expense.PaymentReference,
            expense.PaymentDate,
            expense.IsPaid,
            expense.RecordedByUserId,
            recordedByName,
            expense.CreatedAtUtc,
            expense.ApprovedByUserId,
            expense.ApprovedByUserId is null ? null : userNames.GetValueOrDefault(expense.ApprovedByUserId.Value),
            expense.ApprovedAtUtc,
            expense.ApprovalComments,
            expense.RecurringExpenseId is not null,
            expense.IsEditable,
            [.. expense.Attachments
                .OrderBy(a => a.UploadedAtUtc)
                .Select(a => new ExpenseAttachmentDto(
                    a.Id, a.FileName, a.ContentType, a.SizeBytes, a.UploadedAtUtc,
                    userNames.GetValueOrDefault(a.UploadedByUserId, string.Empty)))],
            [.. expense.ApprovalTrail
                .OrderBy(e => e.ActedAtUtc)
                .Select(e => new ExpenseApprovalEntryDto(
                    e.FromStatus, e.ToStatus, e.ActedByUserId,
                    userNames.GetValueOrDefault(e.ActedByUserId, string.Empty), e.ActedAtUtc, e.Comments))]);
    }

    public static ExpenseSummaryDto ToSummaryDto(
        this Expense expense, string categoryName, string recordedByName)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new ExpenseSummaryDto(
            expense.Id,
            expense.ExpenseNumber,
            expense.ExpenseDate,
            expense.CategoryId,
            categoryName,
            expense.Amount,
            expense.Description,
            expense.Status,
            expense.PaymentMethod,
            expense.PaymentReference,
            expense.IsPaid,
            expense.RecurringExpenseId is not null,
            expense.Attachments.Count,
            recordedByName);
    }

    public static RecurringExpenseDto ToDto(this RecurringExpense recurring, string categoryName)
    {
        ArgumentNullException.ThrowIfNull(recurring);

        return new RecurringExpenseDto(
            recurring.Id,
            recurring.CategoryId,
            categoryName,
            recurring.Amount,
            recurring.Description,
            recurring.PaymentMethod,
            recurring.DayOfMonth,
            recurring.IsActive,
            recurring.LastGeneratedYear,
            recurring.LastGeneratedMonth);
    }
}
