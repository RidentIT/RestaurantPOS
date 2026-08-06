using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.RecordSupplierPayment;

public sealed record RecordSupplierPaymentCommand(
    Guid PurchaseOrderId,
    decimal Amount,
    DateTime PaymentDateUtc,
    PaymentMethod Method,
    string? InvoiceReference,
    string? Notes) : IRequest<Result<SupplierPaymentDto>>;

public sealed class RecordSupplierPaymentCommandValidator : AbstractValidator<RecordSupplierPaymentCommand>
{
    public RecordSupplierPaymentCommandValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Payment amount must be greater than zero.");
        RuleFor(x => x.Method).IsInEnum().WithMessage("Select a valid payment method.");
        RuleFor(x => x.InvoiceReference).MaximumLength(SupplierPayment.InvoiceReferenceMaxLength);
        RuleFor(x => x.Notes).MaximumLength(SupplierPayment.NotesMaxLength);
    }
}

internal sealed class RecordSupplierPaymentCommandHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<RecordSupplierPaymentCommand, Result<SupplierPaymentDto>>
{
    public async Task<Result<SupplierPaymentDto>> Handle(
        RecordSupplierPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await db.PurchaseOrders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.PurchaseOrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<SupplierPaymentDto>(SupplierErrors.PurchaseOrderNotFound(request.PurchaseOrderId));
        }

        var alreadyPaid = await db.SupplierPayments
            .Where(p => p.PurchaseOrderId == request.PurchaseOrderId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var balance = order.TotalAmount - alreadyPaid;

        if (request.Amount > balance)
        {
            return Result.Failure<SupplierPaymentDto>(SupplierErrors.PaymentExceedsBalance(balance));
        }

        var payment = SupplierPayment.Create(
            request.PurchaseOrderId,
            request.Amount,
            request.PaymentDateUtc,
            request.Method,
            currentUser.UserId!.Value,
            request.InvoiceReference,
            request.Notes);

        db.SupplierPayments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        var recordedByName = await db.Users.Where(u => u.Id == payment.RecordedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        return Result.Success(new SupplierPaymentDto(
            payment.Id, payment.PurchaseOrderId, payment.Amount, payment.PaymentDateUtc, payment.Method,
            payment.InvoiceReference, payment.RecordedByUserId, recordedByName, payment.Notes));
    }
}