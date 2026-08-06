namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>Abstracts the system clock so time-dependent behaviour can be tested.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    /// <summary>
    /// The restaurant's own date, in the machine's local time.
    /// </summary>
    /// <remarks>
    /// Anything a human reads as "today" has to come from here rather than be derived from
    /// <see cref="UtcNow"/>. The restaurant runs on Sri Lanka time, five and a half hours ahead of
    /// UTC, so a UTC date would roll the daily order sequence over at half past five in the
    /// morning — restarting the numbering mid-service and dating a late-evening order to the day
    /// before.
    /// </remarks>
    DateOnly Today { get; }
}