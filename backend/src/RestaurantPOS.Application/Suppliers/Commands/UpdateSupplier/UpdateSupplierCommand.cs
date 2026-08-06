using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.UpdateSupplier;

public sealed record UpdateSupplierCommand(
    Guid SupplierId,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays) : IRequest<Result<SupplierDto>>;

public sealed class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(Supplier.NameMaxLength);

        RuleFor(x => x.ContactName).MaximumLength(Supplier.ContactNameMaxLength);
        RuleFor(x => x.Phone).MaximumLength(Supplier.PhoneMaxLength);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(Supplier.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Address).MaximumLength(Supplier.AddressMaxLength);

        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0)
            .WithMessage("Payment terms cannot be a negative number of days.");

        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0)
            .WithMessage("Credit limit cannot be negative.")
            .When(x => x.CreditLimit is not null);

        RuleFor(x => x.LeadTimeDays).GreaterThanOrEqualTo(0)
            .WithMessage("Lead time cannot be a negative number of days.")
            .When(x => x.LeadTimeDays is not null);
    }
}

internal sealed class UpdateSupplierCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == request.SupplierId, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure<SupplierDto>(SupplierErrors.NotFound(request.SupplierId));
        }

        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var nameTaken = await db.Suppliers.AnyAsync(
            s => s.Id != request.SupplierId && s.Name.ToLower() == normalised, cancellationToken);

        if (nameTaken)
        {
            return Result.Failure<SupplierDto>(SupplierErrors.NameTaken);
        }

        supplier.UpdateDetails(
            name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address,
            request.PaymentTermsDays,
            request.CreditLimit,
            request.LeadTimeDays);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(supplier.ToDto());
    }
}