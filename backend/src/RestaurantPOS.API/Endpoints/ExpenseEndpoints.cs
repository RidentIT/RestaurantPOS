using MediatR;

using Microsoft.AspNetCore.Mvc;

using RestaurantPOS.API.Contracts.Expenses;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Expenses.Commands.AddExpenseAttachment;
using RestaurantPOS.Application.Expenses.Commands.CreateExpense;
using RestaurantPOS.Application.Expenses.Commands.CreateExpenseCategory;
using RestaurantPOS.Application.Expenses.Commands.DecideExpenses;
using RestaurantPOS.Application.Expenses.Commands.DeleteExpense;
using RestaurantPOS.Application.Expenses.Commands.DeleteExpenseCategory;
using RestaurantPOS.Application.Expenses.Commands.GenerateDueRecurringExpenses;
using RestaurantPOS.Application.Expenses.Commands.RemoveExpenseAttachment;
using RestaurantPOS.Application.Expenses.Commands.SaveRecurringExpense;
using RestaurantPOS.Application.Expenses.Commands.SetExpenseCategoryActive;
using RestaurantPOS.Application.Expenses.Commands.SetExpensePaid;
using RestaurantPOS.Application.Expenses.Commands.SetRecurringExpenseActive;
using RestaurantPOS.Application.Expenses.Commands.SubmitExpense;
using RestaurantPOS.Application.Expenses.Commands.UpdateExpense;
using RestaurantPOS.Application.Expenses.Commands.UpdateExpenseCategory;
using RestaurantPOS.Application.Expenses.Queries.GetDailyExpenseReport;
using RestaurantPOS.Application.Expenses.Queries.GetExpenseAttachment;
using RestaurantPOS.Application.Expenses.Queries.GetExpenseById;
using RestaurantPOS.Application.Expenses.Queries.GetExpenseCategories;
using RestaurantPOS.Application.Expenses.Queries.GetExpenseRangeReport;
using RestaurantPOS.Application.Expenses.Queries.GetExpenses;
using RestaurantPOS.Application.Expenses.Queries.GetMonthlyExpenseReport;
using RestaurantPOS.Application.Expenses.Queries.GetRecurringExpenses;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Expense recording, approval, categories, recurring costs and reporting.</summary>
public static class ExpenseEndpoints
{
    /// <summary>Largest receipt accepted, matching the limit the command enforces.</summary>
    private const long MaxAttachmentBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var group = routes.MapGroup("/expenses")
            .WithTags("Expenses")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.ExpensesManagement));

        MapCategories(group);
        MapExpenses(group);
        MapApproval(group);
        MapAttachments(group);
        MapRecurring(group);
        MapReports(group);

        return routes;
    }

    private static void MapCategories(RouteGroupBuilder group)
    {
        group.MapGet("/categories", async (bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetExpenseCategoriesQuery(isActive), ct);
                return result.ToHttpResult();
            })
            .WithName("GetExpenseCategories")
            .WithSummary("Every expense category, built-in and custom.");

        group.MapPost("/categories", async (
                CreateExpenseCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateExpenseCategoryCommand(
                    request.Name, request.Description, request.MonthlyBudget, request.ParentCategoryId);

                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(c => $"/api/v1/expenses/categories/{c.Id}");
            })
            .WithName("CreateExpenseCategory")
            .WithSummary("Adds a custom expense category.");

        group.MapPut("/categories/{id:guid}", async (
                Guid id, UpdateExpenseCategoryRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateExpenseCategoryCommand(
                    id, request.Name, request.Description, request.MonthlyBudget, request.ParentCategoryId);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateExpenseCategory")
            .WithSummary("Renames a category or changes its monthly budget.");

        group.MapPut("/categories/{id:guid}/status", async (
                Guid id, SetExpenseCategoryActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetExpenseCategoryActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetExpenseCategoryActive")
            .WithSummary("Retires a category or brings it back into use.");

        group.MapDelete("/categories/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteExpenseCategoryCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("DeleteExpenseCategory")
            .WithSummary("Removes a custom category that has never been used.");
    }

    private static void MapExpenses(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                DateOnly? from,
                DateOnly? to,
                Guid? categoryId,
                ExpensePaymentMethod? paymentMethod,
                ExpenseStatus? status,
                bool? isPaid,
                string? search,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetExpensesQuery(from, to, categoryId, paymentMethod, status, isPaid, search);
                var result = await sender.Send(query, ct);

                return result.ToHttpResult();
            })
            .WithName("GetExpenses")
            .WithSummary("Lists expenses with every filter the screens offer.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetExpenseByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetExpenseById")
            .WithSummary("One expense with its attachments and approval history.");

        group.MapPost("/", async (CreateExpenseRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateExpenseCommand(
                    request.ExpenseDate,
                    request.CategoryId,
                    request.Amount,
                    request.Description,
                    request.PaymentMethod,
                    request.PaymentReference,
                    request.PaymentDate,
                    request.IsPaid,
                    request.SubmitForApproval);

                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(e => $"/api/v1/expenses/{e.Id}");
            })
            .WithName("CreateExpense")
            .WithSummary("Records money paid out.");

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateExpenseRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateExpenseCommand(
                    id,
                    request.ExpenseDate,
                    request.CategoryId,
                    request.Amount,
                    request.Description,
                    request.PaymentMethod,
                    request.PaymentReference,
                    request.PaymentDate,
                    request.IsPaid);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateExpense")
            .WithSummary("Corrects an expense that has not yet been approved.");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DeleteExpenseCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("DeleteExpense")
            .WithSummary("Discards an expense that was never ruled on.");

        group.MapPut("/{id:guid}/paid", async (
                Guid id, SetExpensePaidRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SetExpensePaidCommand(id, request.IsPaid, request.PaymentDate), ct);

                return result.ToHttpResult();
            })
            .WithName("SetExpensePaid")
            .WithSummary("Records whether the money has actually gone out.");
    }

    private static void MapApproval(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/submit", async (
                Guid id, SubmitExpenseRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SubmitExpenseCommand(id, request.Comments), ct);
                return result.ToHttpResult();
            })
            .WithName("SubmitExpense")
            .WithSummary("Puts a draft expense forward for approval.");

        group.MapPost("/approve", async (
                DecideExpensesRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new DecideExpensesCommand(request.ExpenseIds, Approve: true, request.Comments), ct);

                return result.ToHttpResult();
            })
            .WithName("ApproveExpenses")
            .WithSummary("Approves one or more expenses in a single decision.");

        group.MapPost("/reject", async (
                DecideExpensesRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new DecideExpensesCommand(request.ExpenseIds, Approve: false, request.Comments), ct);

                return result.ToHttpResult();
            })
            .WithName("RejectExpenses")
            .WithSummary("Rejects one or more expenses, keeping them on record.");
    }

    private static void MapAttachments(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/attachments", async (
                Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();

                var command = new AddExpenseAttachmentCommand(
                    id, stream, file.FileName, file.ContentType ?? "application/octet-stream", file.Length);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("AddExpenseAttachment")
            .WithSummary("Files a receipt or invoice against an expense.")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxAttachmentBytes));

        group.MapGet("/attachments/{attachmentId:guid}", async (
                Guid attachmentId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetExpenseAttachmentQuery(attachmentId), ct);

                if (result.IsFailure)
                {
                    return result.ToHttpResult();
                }

                var attachment = result.Value;

                // Inline so a receipt opens in a viewer rather than forcing a download, which is
                // what somebody checking an expense actually wants.
                return Results.File(attachment.Content, attachment.ContentType, attachment.FileName);
            })
            .WithName("GetExpenseAttachment")
            .WithSummary("Opens a filed receipt.");

        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", async (
                Guid id, Guid attachmentId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RemoveExpenseAttachmentCommand(id, attachmentId), ct);
                return result.ToHttpResult();
            })
            .WithName("RemoveExpenseAttachment")
            .WithSummary("Removes a filed receipt.");
    }

    private static void MapRecurring(RouteGroupBuilder group)
    {
        group.MapGet("/recurring", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRecurringExpensesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithName("GetRecurringExpenses")
            .WithSummary("Standing monthly costs such as rent and salaries.");

        group.MapPost("/recurring", async (
                SaveRecurringExpenseRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new SaveRecurringExpenseCommand(
                    null, request.CategoryId, request.Amount, request.Description,
                    request.PaymentMethod, request.DayOfMonth);

                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(r => $"/api/v1/expenses/recurring/{r.Id}");
            })
            .WithName("CreateRecurringExpense")
            .WithSummary("Sets up a standing monthly cost.");

        group.MapPut("/recurring/{id:guid}", async (
                Guid id, SaveRecurringExpenseRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new SaveRecurringExpenseCommand(
                    id, request.CategoryId, request.Amount, request.Description,
                    request.PaymentMethod, request.DayOfMonth);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateRecurringExpense")
            .WithSummary("Changes a standing monthly cost.");

        group.MapPut("/recurring/{id:guid}/status", async (
                Guid id, SetRecurringExpenseActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new SetRecurringExpenseActiveCommand(id, request.IsActive), ct);

                return result.ToHttpResult();
            })
            .WithName("SetRecurringExpenseActive")
            .WithSummary("Stops or resumes a standing monthly cost.");

        group.MapPost("/recurring/generate", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GenerateDueRecurringExpensesCommand(), ct);
                return result.ToHttpResult();
            })
            .WithName("GenerateDueRecurringExpenses")
            .WithSummary("Creates any recurring expense still owed for this month. Safe to call repeatedly.");
    }

    private static void MapReports(RouteGroupBuilder group)
    {
        group.MapGet("/reports/daily", async (DateOnly date, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetDailyExpenseReportQuery(date), ct);
                return result.ToHttpResult();
            })
            .WithName("GetDailyExpenseReport")
            .WithSummary("A day's expenses against that day's takings.");

        group.MapGet("/reports/monthly", async (
                int year, int month, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMonthlyExpenseReportQuery(year, month), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMonthlyExpenseReport")
            .WithSummary("A month's expenses, trend, weekly split, comparison and budget alerts.");

        group.MapGet("/reports/range", async (
                DateOnly from, DateOnly to, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetExpenseRangeReportQuery(from, to), ct);
                return result.ToHttpResult();
            })
            .WithName("GetExpenseRangeReport")
            .WithSummary("Expenses, revenue and profit across any span of dates.");
    }
}
