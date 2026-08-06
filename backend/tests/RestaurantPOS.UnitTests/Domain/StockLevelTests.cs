using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class StockLevelTests
{
    [Fact]
    public void NewLevel_StartsAtZero()
    {
        var level = new StockLevel(Guid.NewGuid(), StoreType.MainStore);

        level.QuantityOnHand.Should().Be(0);
    }

    [Fact]
    public void ApplyDelta_IncreasesTheBalance()
    {
        var level = new StockLevel(Guid.NewGuid(), StoreType.MainStore);
        var now = DateTime.UtcNow;

        level.ApplyDelta(10m, now);

        level.QuantityOnHand.Should().Be(10m);
        level.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void ApplyDelta_AccumulatesAcrossMultipleCalls()
    {
        var level = new StockLevel(Guid.NewGuid(), StoreType.MainStore);
        var now = DateTime.UtcNow;

        level.ApplyDelta(10m, now);
        level.ApplyDelta(-3m, now);

        level.QuantityOnHand.Should().Be(7m);
    }

    [Fact]
    public void ApplyDelta_RefusesToGoNegative()
    {
        var level = new StockLevel(Guid.NewGuid(), StoreType.MainStore);
        var now = DateTime.UtcNow;
        level.ApplyDelta(5m, now);

        var act = () => level.ApplyDelta(-10m, now);

        act.Should().Throw<InvalidOperationException>(
            "this is a last-resort guard; the application layer is expected to check first");
        level.QuantityOnHand.Should().Be(5m, "a rejected delta must not partially apply");
    }
}