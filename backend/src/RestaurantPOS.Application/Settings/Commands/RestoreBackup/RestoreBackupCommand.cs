using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Authentication.Commands.VerifyApprovalPin;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Settings.Commands.RestoreBackup;

/// <summary>
/// Replaces every record in the database with what an archive holds.
/// </summary>
/// <remarks>
/// Two separate confirmations stand in front of this: an administrator's PIN, and typing the word
/// "RESTORE" by hand — a PIN alone is also what unlocks a routine order cancellation, and this is
/// nowhere near routine. The API endpoint shuts the whole application down immediately after this
/// succeeds, so nothing else in this process ever touches the connections that used to point at
/// the data this just erased.
/// </remarks>
public sealed record RestoreBackupCommand(string FileName, string Pin, string ConfirmationText)
    : IRequest<Result>;

public sealed class RestoreBackupCommandValidator : AbstractValidator<RestoreBackupCommand>
{
    public RestoreBackupCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.Pin).NotEmpty();
    }
}

internal sealed class RestoreBackupCommandHandler(IAppDbContext db, IBackupService backupService, ISender sender)
    : IRequestHandler<RestoreBackupCommand, Result>
{
    public async Task<Result> Handle(RestoreBackupCommand request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.ConfirmationText, "RESTORE", StringComparison.Ordinal))
        {
            return Result.Failure(SettingsErrors.RestoreConfirmationMismatch);
        }

        var approval = await sender.Send(
            new VerifyApprovalPinCommand(request.Pin, "Restore from backup"), cancellationToken);

        if (approval.IsFailure)
        {
            return Result.Failure(approval.Error);
        }

        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        var known = await backupService.ListAsync(settings.BackupFolderPath, cancellationToken);
        if (!known.Any(b => string.Equals(b.FileName, request.FileName, StringComparison.Ordinal)))
        {
            return Result.Failure(SettingsErrors.BackupNotFound(request.FileName));
        }

        try
        {
            await backupService.RestoreAsync(settings.BackupFolderPath, request.FileName, cancellationToken);
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested is false)
        {
            return Result.Failure(SettingsErrors.RestoreFailed);
        }

        return Result.Success();
    }
}
