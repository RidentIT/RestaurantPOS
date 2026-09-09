using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Settings.Commands.UpdateDefaultPrinter;

/// <summary>
/// Which printer receipts go to (<paramref name="PrinterName"/>) and which kitchen tickets go to
/// (<paramref name="KitchenPrinterName"/>). Either being null falls back — the kitchen printer to
/// the receipt printer, the receipt printer to the till's own Windows default.
/// </summary>
public sealed record UpdateDefaultPrinterCommand(string? PrinterName, string? KitchenPrinterName)
    : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateDefaultPrinterCommandValidator : AbstractValidator<UpdateDefaultPrinterCommand>
{
    public UpdateDefaultPrinterCommandValidator()
    {
        RuleFor(x => x.PrinterName).MaximumLength(RestaurantSettings.PrinterNameMaxLength);
        RuleFor(x => x.KitchenPrinterName).MaximumLength(RestaurantSettings.PrinterNameMaxLength);
    }
}

internal sealed class UpdateDefaultPrinterCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateDefaultPrinterCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateDefaultPrinterCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdatePrinters(request.PrinterName, request.KitchenPrinterName);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
