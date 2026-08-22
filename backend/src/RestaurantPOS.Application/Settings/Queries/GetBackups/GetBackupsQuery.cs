using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Application.Settings.Dtos;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Queries.GetBackups;

/// <summary>Every backup archive on disk, newest first.</summary>
public sealed record GetBackupsQuery : IRequest<Result<IReadOnlyCollection<BackupDto>>>;

internal sealed class GetBackupsQueryHandler(IAppDbContext db, IBackupService backupService)
    : IRequestHandler<GetBackupsQuery, Result<IReadOnlyCollection<BackupDto>>>
{
    public async Task<Result<IReadOnlyCollection<BackupDto>>> Handle(
        GetBackupsQuery request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);
        var backups = await backupService.ListAsync(settings.BackupFolderPath, cancellationToken);

        IReadOnlyCollection<BackupDto> dtos =
            [.. backups
                .OrderByDescending(b => b.CreatedAtUtc)
                .Select(b => new BackupDto(b.FileName, b.CreatedAtUtc, b.SizeBytes))];

        return Result.Success(dtos);
    }
}
