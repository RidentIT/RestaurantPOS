using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Expenses.Common;
using RestaurantPOS.Application.Expenses.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Expenses.Queries.GetExpenseById;

/// <summary>One expense in full, with its attachments and approval history.</summary>
public sealed record GetExpenseByIdQuery(Guid ExpenseId) : IRequest<Result<ExpenseDto>>;

internal sealed class GetExpenseByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetExpenseByIdQuery, Result<ExpenseDto>>
{
    public async Task<Result<ExpenseDto>> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await ExpenseRepository.WithAggregate(db.Expenses.AsNoTracking())
            .FirstOrDefaultAsync(e => e.Id == request.ExpenseId, cancellationToken);

        if (expense is null)
        {
            return Result.Failure<ExpenseDto>(ExpenseErrors.NotFound(request.ExpenseId));
        }

        return Result.Success(await ExpenseResultFactory.BuildAsync(db, expense, cancellationToken));
    }
}
