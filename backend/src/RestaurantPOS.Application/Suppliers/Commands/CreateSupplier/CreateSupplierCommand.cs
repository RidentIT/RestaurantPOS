using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Suppliers.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Suppliers.Commands.CreateSupplier;

public sealed record CreateSupplierCommand(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    int PaymentTermsDays,
    decimal? CreditLimit,
    int? LeadTimeDays) : IRequest<Result<SupplierDto>>;

public sealed class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
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

internal sealed class CreateSupplierCommandHandler(IAppDbContext db)
    : IRequestHandler<CreateSupplierCommand, Result<SupplierDto>>
{
    public async Task<Result<SupplierDto>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var normalised = name.ToLowerInvariant();

        var exists = await db.Suppliers.AnyAsync(s => s.Name.ToLower() == normalised, cancellationToken);
        if (exists)
        {
            return Result.Failure<SupplierDto>(SupplierErrors.NameTaken);
        }

        var supplier = Supplier.Create(
            name,
            request.ContactName,
            request.Phone,
            request.Email,
            request.Address,
            request.PaymentTermsDays,
            request.CreditLimit,
            request.LeadTimeDays);

        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(supplier.ToDto());
    }
}