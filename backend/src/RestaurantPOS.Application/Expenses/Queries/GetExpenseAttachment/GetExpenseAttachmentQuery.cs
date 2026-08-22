using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Queries.GetExpenseAttachment;

/// <summary>Opens a filed receipt so it can be viewed or downloaded.</summary>
public sealed record GetExpenseAttachmentQuery(Guid AttachmentId) : IRequest<Result<StoredAttachment>>;

internal sealed class GetExpenseAttachmentQueryHandler(IAppDbContext db, IExpenseAttachmentStore store)
    : IRequestHandler<GetExpenseAttachmentQuery, Result<StoredAttachment>>
{
    public async Task<Result<StoredAttachment>> Handle(
        GetExpenseAttachmentQuery request, CancellationToken cancellationToken)
    {
        var attachment = await db.ExpenseAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId, cancellationToken);

        if (attachment is null)
        {
            return Result.Failure<StoredAttachment>(ExpenseErrors.AttachmentNotFound(request.AttachmentId));
        }

        var stored = await store.OpenAsync(
            attachment.StoredPath, attachment.ContentType, attachment.FileName, cancellationToken);

        // The row survived but the file did not — someone tidied the folder, or a restore brought
        // back the database without the attachments beside it.
        return stored is null
            ? Result.Failure<StoredAttachment>(ExpenseErrors.AttachmentMissing)
            : Result.Success(stored);
    }
}
