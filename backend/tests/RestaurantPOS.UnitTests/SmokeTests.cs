using FluentAssertions;

using Xunit;

namespace RestaurantPOS.UnitTests;

public class SmokeTests
{
    [Fact]
    public void Sanity_Check_Passes()
    {
        var result = 2 + 2;

        result.Should().Be(4);
    }
}