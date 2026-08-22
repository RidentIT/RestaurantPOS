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
/// The printer bills and KOTs go to. Null falls back to the system default — the till's own
/// Windows printer, whatever that happens to be.
/// </summary>
public sealed record UpdateDefaultPrinterCommand(string? PrinterName) : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateDefaultPrinterCommandValidator : AbstractValidator<UpdateDefaultPrinterCommand>
{
    public UpdateDefaultPrinterCommandValidator() =>
        RuleFor(x => x.PrinterName).MaximumLength(RestaurantSettings.PrinterNameMaxLength);
}

internal sealed class UpdateDefaultPrinterCommandHandler(IAppDbContext db)
    : IRequestHandler<UpdateDefaultPrinterCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateDefaultPrinterCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateDefaultPrinter(request.PrinterName);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
