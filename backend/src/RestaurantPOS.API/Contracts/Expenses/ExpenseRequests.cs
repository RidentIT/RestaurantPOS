using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Expenses;

public sealed record CreateExpenseCategoryRequest(
    string Name, string? Description, decimal? MonthlyBudget, Guid? ParentCategoryId);

public sealed record UpdateExpenseCategoryRequest(
    string Name, string? Description, decimal? MonthlyBudget, Guid? ParentCategoryId);

public sealed record SetExpenseCategoryActiveRequest(bool IsActive);

public sealed record CreateExpenseRequest(
    DateOnly ExpenseDate,
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid = false,
    bool SubmitForApproval = false);

public sealed record UpdateExpenseRequest(
    DateOnly ExpenseDate,
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    string? PaymentReference,
    DateOnly? PaymentDate,
    bool IsPaid);

public sealed record SubmitExpenseRequest(string? Comments);

/// <summary>
/// Takes a list so the day's expenses can be signed off together, which is how a manager actually
/// reviews them. A single decision is a list of one.
/// </summary>
public sealed record DecideExpensesRequest(IReadOnlyCollection<Guid> ExpenseIds, string? Comments);

public sealed record SetExpensePaidRequest(bool IsPaid, DateOnly? PaymentDate);

public sealed record SaveRecurringExpenseRequest(
    Guid CategoryId,
    decimal Amount,
    string? Description,
    ExpensePaymentMethod PaymentMethod,
    int DayOfMonth);

public sealed record SetRecurringExpenseActiveRequest(bool IsActive);
