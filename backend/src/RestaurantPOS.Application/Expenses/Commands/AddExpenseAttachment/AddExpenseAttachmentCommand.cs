using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Commands.AddExpenseAttachment;

/// <summary>Files a receipt or invoice against an expense (EXP-037).</summary>
public sealed record AddExpenseAttachmentCommand(
    Guid ExpenseId, Stream Content, string FileName, string ContentType, long SizeBytes)
    : IRequest<Result<ExpenseDto>>;

internal sealed class AddExpenseAttachmentCommandHandler(
    IAppDbContext db, IExpenseAttachmentStore store, ICurrentUser currentUser, IDateTimeProvider clock)
    : IRequestHandler<AddExpenseAttachmentCommand, Result<ExpenseDto>>
{
    private const int MaxMegabytes = 10;
    private const long MaxSizeBytes = MaxMegabytes * 1024L * 1024L;

    /// <summary>
    /// What a receipt is allowed to be. Restricted deliberately: these files are served back to a
    /// browser, and anything outside this list is either useless as a receipt or a liability to
    /// hand back to a client.
    /// </summary>
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
    };

    public async Task<Result<ExpenseDto>> Handle(
        AddExpenseAttachmentCommand request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.FindAsync(db, request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        if (!expense.IsEditable)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotEditable);
        }

        if (!AllowedContentTypes.Contains(request.ContentType))
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.AttachmentTypeNotAllowed);
        }

        if (request.SizeBytes > MaxSizeBytes)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.AttachmentTooLarge(MaxMegabytes));
        }

        var storedPath = await store.SaveAsync(
            request.Content, request.FileName, expense.ExpenseDate, cancellationToken);

        expense.AddAttachment(
            request.FileName,
            storedPath,
            request.ContentType,
            request.SizeBytes,
            currentUser.UserId!.Value,
            clock.UtcNow);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
