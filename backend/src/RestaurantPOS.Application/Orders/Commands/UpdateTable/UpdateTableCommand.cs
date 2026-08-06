using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.UpdateTable;

public sealed record UpdateTableCommand(Guid TableId, string Number, int Seats, string? Notes)
    : IRequest<Result<TableDto>>;

public sealed class UpdateTableCommandValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableCommandValidator()
    {
        RuleFor(x => x.TableId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(RestaurantTable.NumberMaxLength);
        RuleFor(x => x.Seats).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(RestaurantTable.NotesMaxLength);
    }
}

internal sealed class UpdateTableCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateTableCommand, Result<TableDto>>
{
    public async Task<Result<TableDto>> Handle(UpdateTableCommand request, CancellationToken cancellationToken)
    {
        var table = await db.RestaurantTables.FirstOrDefaultAsync(t => t.Id == request.TableId, cancellationToken);

        if (table is null)
        {
            return Result.Failure<TableDto>(OrderErrors.TableNotFound(request.TableId));
        }

        var number = request.Number.Trim();

        var taken = await db.RestaurantTables
            .AnyAsync(t => t.Id != request.TableId && t.Number.ToLower() == number.ToLower(), cancellationToken);

        if (taken)
        {
            return Result.Failure<TableDto>(OrderErrors.TableNumberTaken);
        }

        table.UpdateDetails(number, request.Seats, request.Notes);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TableDto(
            table.Id, table.Number, table.Seats, table.Notes, table.IsActive, CurrentOrder: null));
    }
}
