using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Inventory.Common;
using RestaurantPOS.Application.Orders.Common;
using RestaurantPOS.Application.Orders.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Orders.Commands.CompleteOrderPayment;

/// <summary>One tender offered against the bill.</summary>
/// <param name="TenderedAmount">
/// Cash actually handed over, when it is more than <paramref name="Amount"/>. The difference is
/// the change (POS-024). Null for anything that cannot be over-tendered, like a card.
/// </param>
public sealed record OrderPaymentInput(
    OrderPaymentMethod Method, decimal Amount, decimal? TenderedAmount, string? Reference);

/// <summary>
/// Settles a bill and issues the receipt (POS-025), releasing the table (POS-032).
/// </summary>
/// <remarks>
/// This is also where a sale finally reaches the inventory: the dishes on the bill are turned
/// into kitchen stock deductions through each one's recipe (REC-007). Deducting at payment rather
/// than when the KOT prints is what makes voided items cost nothing (BR-POS-014) — a line taken
/// off the bill is simply not on it by the time this runs.
/// </remarks>
public sealed record CompleteOrderPaymentCommand(Guid OrderId, IReadOnlyCollection<OrderPaymentInput> Payments)
    : IRequest<Result<ReceiptDocumentDto>>;

public sealed class CompleteOrderPaymentCommandValidator : AbstractValidator<CompleteOrderPaymentCommand>
{
    public CompleteOrderPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Payments).NotEmpty().WithMessage("Record at least one payment.");

        RuleForEach(x => x.Payments).ChildRules(payment =>
        {
            payment.RuleFor(p => p.Amount).GreaterThan(0).WithMessage("A payment must be greater than zero.");

            payment.RuleFor(p => p.TenderedAmount)
                .GreaterThanOrEqualTo(p => p.Amount)
                .When(p => p.TenderedAmount.HasValue)
                .WithMessage("The amount tendered cannot be less than the amount it settles.");
        });
    }
}

internal sealed class CompleteOrderPaymentCommandHandler(
    IAppDbContext db, IDateTimeProvider clock, ICurrentUser currentUser, IRestaurantProfile restaurant)
    : IRequestHandler<CompleteOrderPaymentCommand, Result<ReceiptDocumentDto>>
{
    public async Task<Result<ReceiptDocumentDto>> Handle(
        CompleteOrderPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await OrderRepository.FindAsync(db, request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<ReceiptDocumentDto>(OrderErrors.NotFound(request.OrderId));
        }

        if (order.Status != OrderStatus.Checkout)
        {
            return Result.Failure<ReceiptDocumentDto>(OrderErrors.NotInCheckout);
        }

        var offered = request.Payments.Sum(p => p.Amount);

        if (offered != order.Total)
        {
            return Result.Failure<ReceiptDocumentDto>(OrderErrors.PaymentMismatch(order.Total, offered));
        }

        foreach (var payment in request.Payments)
        {
            order.AddPayment(payment.Method, payment.Amount, payment.TenderedAmount, payment.Reference);
        }

        var now = clock.UtcNow;
        var receipt = order.Complete(BuildReceiptNumber(order.OrderNumber, order.OrderDate ?? clock.Today), now);

        var consumption = await SaleStockConsumption.ApplyAsync(
            db,
            [.. order.ActiveItems.Select(i => new SoldItem(i.MenuItemId, i.Quantity))],
            currentUser.UserId!.Value,
            now,
            cancellationToken);

        if (consumption.IsFailure)
        {
            return Result.Failure<ReceiptDocumentDto>(consumption.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(await ReceiptDocumentFactory.BuildAsync(db, order, receipt, restaurant, cancellationToken));
    }

    /// <summary>
    /// Builds the customer-facing reference, e.g. "REC-001-2026" — the order's own number and the
    /// year it was taken, which is enough to find it again from a slip of paper.
    /// </summary>
    private static string BuildReceiptNumber(int? orderNumber, DateOnly orderDate) =>
        $"REC-{orderNumber ?? 0:000}-{orderDate.Year}";
}
