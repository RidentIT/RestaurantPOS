using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.CreateTable;

/// <summary>Adds a table to the floor plan so orders can be taken against it.</summary>
public sealed record CreateTableCommand(string Number, int Seats, string? Notes) : IRequest<Result<TableDto>>;

public sealed class CreateTableCommandValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableCommandValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(RestaurantTable.NumberMaxLength);
        RuleFor(x => x.Seats).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(RestaurantTable.NotesMaxLength);
    }
}

internal sealed class CreateTableCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateTableCommand, Result<TableDto>>
{
    public async Task<Result<TableDto>> Handle(CreateTableCommand request, CancellationToken cancellationToken)
    {
        var number = request.Number.Trim();

        var exists = await db.RestaurantTables
            .AnyAsync(t => t.Number.ToLower() == number.ToLower(), cancellationToken);

        if (exists)
        {
            return Result.Failure<TableDto>(OrderErrors.TableNumberTaken);
        }

        var table = RestaurantTable.Create(number, request.Seats, request.Notes);
        db.RestaurantTables.Add(table);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(new TableDto(
            table.Id, table.Number, table.Seats, table.Notes, table.IsActive, CurrentOrder: null));
    }
}
