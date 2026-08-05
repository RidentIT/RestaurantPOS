using FluentAssertions;

using RestaurantPOS.Domain.Entities;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class MenuItemTests
{
    [Fact]
    public void Create_StartsActive()
    {
        var item = MenuItem.Create("Chicken Fried Rice", "Rice & Curry", 850m);

        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RejectsANegativePrice()
    {
        var act = () => MenuItem.Create("Water", "Beverages", -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_AllowsAZeroPrice()
    {
        var item = MenuItem.Create("Complimentary Water", "Beverages", 0m);

        item.Price.Should().Be(0m);
    }

    [Fact]
    public void UpdateDetails_ChangesNameCategoryAndPrice()
    {
        var item = MenuItem.Create("Fried Rice", "Rice", 800m);

        item.UpdateDetails("Chicken Fried Rice", "Rice & Curry", 850m);

        item.Name.Should().Be("Chicken Fried Rice");
        item.Category.Should().Be("Rice & Curry");
        item.Price.Should().Be(850m);
    }

    [Fact]
    public void ActivateAndDeactivate_ToggleIsActive()
    {
        var item = MenuItem.Create("Fried Rice", "Rice", 800m);

        item.Deactivate();
        item.IsActive.Should().BeFalse();

        item.Activate();
        item.IsActive.Should().BeTrue();
    }
}