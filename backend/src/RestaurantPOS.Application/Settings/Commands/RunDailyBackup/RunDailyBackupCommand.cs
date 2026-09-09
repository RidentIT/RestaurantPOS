using MediatR;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Application.Settings.Common;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Application.Settings.Commands.RunDailyBackup;

/// <summary>
/// Takes today's backup if one has not already been taken today.
/// </summary>
/// <remarks>
/// Called once by the frontend right after a successful sign-in, the same "evaluate on open"
/// approach recurring expenses and notifications use instead of a background scheduler — the till
/// is switched off overnight, so nothing would ever run a scheduled job anyway. Safe to call as
/// often as the app is opened: it looks at what is already on disk rather than remembering when it
/// last ran, so calling it twice in a day does nothing the second time.
/// </remarks>
public sealed record RunDailyBackupCommand : IRequest<Result<bool>>;

internal sealed class RunDailyBackupCommandHandler(IAppDbContext db, IBackupService backupService, IDateTimeProvider clock)
    : IRequestHandler<RunDailyBackupCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RunDailyBackupCommand request, CancellationToken cancellationToken)
    {
        var settings = await RestaurantSettingsAccessor.GetAsync(db, cancellationToken);
        var existing = await backupService.ListAsync(settings.BackupFolderPath, cancellationToken);

        var takenToday = existing.Any(b => DateOnly.FromDateTime(b.CreatedAtUtc.ToLocalTime()) == clock.Today);
        if (takenToday)
        {
            return Result.Success(false);
        }

        await backupService.CreateAsync(settings.BackupFolderPath, settings.BackupRetentionCount, cancellationToken);

        return Result.Success(true);
    }
}
