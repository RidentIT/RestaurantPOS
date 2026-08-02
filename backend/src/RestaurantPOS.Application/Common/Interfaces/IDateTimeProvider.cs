namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>Abstracts the system clock so time-dependent behaviour can be tested.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}