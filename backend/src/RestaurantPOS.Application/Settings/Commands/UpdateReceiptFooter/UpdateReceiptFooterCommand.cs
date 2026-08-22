using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Settings.Commands.UpdateReceiptFooter;

/// <summary>The line printed at the bottom of every receipt, below the QR code.</summary>
public sealed record UpdateReceiptFooterCommand(string Message) : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateReceiptFooterCommandValidator : AbstractValidator<UpdateReceiptFooterCommand>
{
    public UpdateReceiptFooterCommandValidator() =>
        RuleFor(x => x.Message).NotEmpty().MaximumLength(RestaurantSettings.ReceiptFooterMaxLength);
}

internal sealed class UpdateReceiptFooterCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateReceiptFooterCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateReceiptFooterCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateReceiptFooter(request.Message);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
