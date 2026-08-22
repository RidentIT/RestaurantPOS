using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Errors;

namespace RestaurantPOS.Application.Settings.Commands.UpdateBackupSettings;

/// <summary>
/// Where backups are written and how many are kept. Null keeps the default Backups folder beside
/// the app — an admin only needs to set this when pointing backups at a USB drive or a folder a
/// cloud-sync client watches.
/// </summary>
public sealed record UpdateBackupSettingsCommand(string? BackupFolderPath, int RetentionCount)
    : IRequest<Result<RestaurantSettingsDto>>;

public sealed class UpdateBackupSettingsCommandValidator : AbstractValidator<UpdateBackupSettingsCommand>
{
    public UpdateBackupSettingsCommandValidator()
    {
        RuleFor(x => x.BackupFolderPath).MaximumLength(RestaurantSettings.BackupFolderPathMaxLength);
        RuleFor(x => x.RetentionCount).InclusiveBetween(1, 60);
    }
}

internal sealed class UpdateBackupSettingsCommandHandler(IAppDbContext db, IBackupFolderProbe folderProbe)
    : IRequestHandler<UpdateBackupSettingsCommand, Result<RestaurantSettingsDto>>
{
    public async Task<Result<RestaurantSettingsDto>> Handle(
        UpdateBackupSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.BackupFolderPath) && !folderProbe.CanWrite(request.BackupFolderPath))
        {
            return Result.Failure<RestaurantSettingsDto>(SettingsErrors.BackupFolderNotWritable);
        }

        var settings = await RestaurantSettingsAccessor.GetTrackedAsync(db, cancellationToken);

        settings.UpdateBackupSettings(request.BackupFolderPath, request.RetentionCount);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success(settings.ToDto());
    }
}
