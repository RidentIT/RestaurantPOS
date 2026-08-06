using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Clock;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}