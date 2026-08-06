using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Errors;

/// <summary>Errors raised by expense, category, recurring-expense and expense-report use cases.</summary>
public static class ExpenseErrors
{
    public static Error CategoryNotFound(Guid id) =>
        Error.NotFound("ExpenseCategory.NotFound", $"No expense category was found with id '{id}'.");

    public static readonly Error CategoryNameTaken =
        Error.Conflict("ExpenseCategory.NameTaken", "An expense category with that name already exists.");

    public static readonly Error CategoryInactive =
        Error.Validation("ExpenseCategory.Inactive", "This category is inactive and cannot take new expenses.");

    public static readonly Error CategoryIsSystem =
        Error.Conflict(
            "ExpenseCategory.IsSystem",
            "This is a built-in category and cannot be deleted. Deactivate it instead.");

    public static readonly Error CategoryInUse =
        Error.Conflict(
            "ExpenseCategory.InUse",
            "Expenses have already been recorded against this category. Deactivate it instead of deleting it.");

    public static readonly Error CategoryOwnParent =
        Error.Validation("ExpenseCategory.OwnParent", "A category cannot be its own parent.");

    public static readonly Error CategoryNestingTooDeep =
        Error.Validation(
            "ExpenseCategory.NestingTooDeep",
            "Subcategories can only be one level deep. Choose a top-level category as the parent.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Expense.NotFound", $"No expense was found with id '{id}'.");

    public static readonly Error NotEditable =
        Error.Conflict(
            "Expense.NotEditable", "This expense has already been approved or rejected and can no longer be changed.");

    public static readonly Error NotSubmittable =
        Error.Conflict("Expense.NotSubmittable", "Only a draft expense can be submitted for approval.");

    public static readonly Error AlreadyDecided =
        Error.Conflict("Expense.AlreadyDecided", "This expense has already been approved or rejected.");

    public static readonly Error FutureDate =
        Error.Validation("Expense.FutureDate", "An expense cannot be dated in the future.");

    public static readonly Error ReferenceRequired =
        Error.Validation(
            "Expense.ReferenceRequired",
            "A payment reference is required for cheque, card and bank transfer payments.");

    public static Error AttachmentNotFound(Guid id) =>
        Error.NotFound("Expense.AttachmentNotFound", $"No attachment was found with id '{id}'.");

    public static readonly Error AttachmentTypeNotAllowed =
        Error.Validation(
            "Expense.AttachmentTypeNotAllowed", "Only JPEG, PNG, WebP and PDF receipts can be attached.");

    public static Error AttachmentTooLarge(int maxMegabytes) =>
        Error.Validation("Expense.AttachmentTooLarge", $"A receipt cannot be larger than {maxMegabytes} MB.");

    public static readonly Error AttachmentMissing =
        Error.NotFound("Expense.AttachmentMissing", "The stored file for this attachment could not be found.");

    public static Error RecurringNotFound(Guid id) =>
        Error.NotFound("RecurringExpense.NotFound", $"No recurring expense was found with id '{id}'.");

    public static readonly Error InvalidDateRange =
        Error.Validation("Expense.InvalidDateRange", "The start of the range must not be after its end.");
}
