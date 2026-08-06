using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Expenses.Common;

/// <summary>
/// Builds the full expense response, gathering the category and the names behind every user id on
/// the approval trail — which is what turns an audit history into something a person can read.
/// </summary>
public static class ExpenseResultFactory
{
    public static async Task<ExpenseDto> BuildAsync(
        IAppDbContext db, Expense expense, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(expense);

        var categoryName = await db.ExpenseCategories
            .Where(c => c.Id == expense.CategoryId)
            .Select(c => c.Name)
            .FirstAsync(cancellationToken);

        var userIds = expense.ApprovalTrail.Select(t => t.ActedByUserId)
            .Concat(expense.Attachments.Select(a => a.UploadedByUserId))
            .Append(expense.RecordedByUserId)
            .Concat(expense.ApprovedByUserId is { } approver ? [approver] : Array.Empty<Guid>())
            .Distinct()
            .ToList();

        var userNames = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);

        return expense.ToDto(
            categoryName, userNames.GetValueOrDefault(expense.RecordedByUserId, string.Empty), userNames);
    }
}
