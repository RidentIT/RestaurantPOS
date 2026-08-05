using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Inventory.Common;
using RestaurantPOS.Application.Inventory.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Inventory.Commands.CreateStockRelease;

public sealed record StockReleaseLineInput(Guid RawMaterialId, decimal Quantity);

/// <summary>
/// Transfers stock from the Main Store to the Kitchen (INV-008 through INV-011). Approval is not
/// a separate pending step: the requesting user's action and an administrator's approval PIN
/// arrive in the same request, verified here before anything moves, so a release only ever
/// exists already approved.
/// </summary>
public sealed record CreateStockReleaseCommand(
    IReadOnlyCollection<StockReleaseLineInput> Lines, string Pin, string? Notes)
    : IRequest<Result<StockReleaseDto>>;

public sealed class CreateStockReleaseCommandValidator : AbstractValidator<CreateStockReleaseCommand>
{
    public CreateStockReleaseCommandValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one raw material line is required.");

        RuleForEach(x => x.Lines).ChildRules(line =>
            line.RuleFor(l => l.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero."));

        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.RawMaterialId).Distinct().Count() == lines.Count)
            .WithMessage("A raw material cannot appear more than once on the same release.")
            .When(x => x.Lines.Count > 0);

        RuleFor(x => x.Pin).NotEmpty().WithMessage("An administrator's approval PIN is required.");

        RuleFor(x => x.Notes).MaximumLength(StockRelease.NotesMaxLength);
    }
}

internal sealed class CreateStockReleaseCommandHandler(
    IAppDbContext db, ICurrentUser currentUser, IDateTimeProvider clock, ISender sender)
    : IRequestHandler<CreateStockReleaseCommand, Result<StockReleaseDto>>
{
    public async Task<Result<StockReleaseDto>> Handle(
        CreateStockReleaseCommand request, CancellationToken cancellationToken)
    {
        var approval = await sender.Send(
            new VerifyApprovalPinCommand(request.Pin, request.Notes), cancellationToken);

        if (approval.IsFailure)
        {
            return Result.Failure<StockReleaseDto>(approval.Error);
        }

        var rawMaterialIds = request.Lines.Select(l => l.RawMaterialId).ToList();
        var rawMaterials = await db.RawMaterials
            .Where(r => rawMaterialIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        if (rawMaterials.Count != rawMaterialIds.Distinct().Count())
        {
            return Result.Failure<StockReleaseDto>(
                InventoryErrors.RawMaterialNotFound(rawMaterialIds.First(id => !rawMaterials.ContainsKey(id))));
        }

        if (rawMaterials.Values.Any(r => !r.IsActive))
        {
            return Result.Failure<StockReleaseDto>(InventoryErrors.RawMaterialInactive);
        }

        var now = clock.UtcNow;

        var release = StockRelease.Create(
            currentUser.UserId!.Value, now, approval.Value.ApprovedByUserId, approval.Value.ApprovedAtUtc, request.Notes);
        db.StockReleases.Add(release);

        var movements = request.Lines
            .SelectMany(l => new[]
            {
                new StockMovementRequest(
                    l.RawMaterialId, StoreType.MainStore, -l.Quantity, StockMovementType.StockReleaseOut, release.Id, null),
                new StockMovementRequest(
                    l.RawMaterialId, StoreType.Kitchen, l.Quantity, StockMovementType.StockReleaseIn, release.Id, null),
            })
            .ToList();

        var ledgerResult = await InventoryLedger.ApplyAsync(db, movements, currentUser.UserId!.Value, now, cancellationToken);
        if (ledgerResult.IsFailure)
        {
            return Result.Failure<StockReleaseDto>(ledgerResult.Error);
        }

        await db.SaveChangesAsync(cancellationToken);

        var lines = request.Lines
            .Select(l => new StockMovementLineDto(
                l.RawMaterialId, rawMaterials[l.RawMaterialId].Name, rawMaterials[l.RawMaterialId].UnitOfMeasurement, l.Quantity))
            .OrderBy(l => l.RawMaterialName)
            .ToList();

        var requestedByName = await db.Users.Where(u => u.Id == release.RequestedByUserId)
            .Select(u => u.FullName).FirstAsync(cancellationToken);

        return Result.Success(new StockReleaseDto(
            release.Id, release.RequestedByUserId, requestedByName, release.RequestedAtUtc,
            release.ApprovedByUserId, approval.Value.ApprovedByName, release.ApprovedAtUtc, release.Notes, lines));
    }
}