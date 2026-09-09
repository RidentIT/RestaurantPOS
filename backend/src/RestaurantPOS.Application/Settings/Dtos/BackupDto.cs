namespace RestaurantPOS.Application.Settings.Dtos;

/// <summary>One backup archive as shown in the Backup &amp; Restore list.</summary>
public sealed record BackupDto(string FileName, DateTime CreatedAtUtc, long SizeBytes);
