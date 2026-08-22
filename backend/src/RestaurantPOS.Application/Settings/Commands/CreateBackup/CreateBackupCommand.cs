using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.CreateBackup;

/// <summary>Takes a backup right now — the "Back up now" button, and the daily auto-backup alike.</summary>
public sealed record CreateBackupCommand : IRequest<Result<BackupDto>>;

internal sealed class CreateBackupCommandHandler(IAppDbContext db, IBackupService backupService)
    : IRequestHandler<CreateBackupCommand, Result<BackupDto>>
{
    public async Task<Result<BackupDto>> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);

        var created = await backupService.CreateAsync(
            settings.BackupFolderPath, settings.BackupRetentionCount, cancellationToken);

        return Result.Success(new BackupDto(created.FileName, created.CreatedAtUtc, created.SizeBytes));
    }
}
