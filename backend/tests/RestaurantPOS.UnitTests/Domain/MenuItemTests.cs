using FluentAssertions;

using RestaurantPOS.Domain.Entities;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class MenuItemTests
{
    private static MenuItemVariantEdit Variant(string? name, decimal price) => new(Id: null, name, price);

    [Fact]
    public void Create_StartsActive()
    {
        var item = MenuItem.Create("Chicken Fried Rice", "Rice & Curry", [Variant(null, 850m)]);

        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_RequiresAtLeastOneVariant()
    {
        var act = () => MenuItem.Create("Water", "Beverages", []);

        act.Should().Throw<ArgumentException>("a menu item must always have at least one size");
    }

    [Fact]
    public void Create_RejectsANegativePrice()
    {
        var act = () => MenuItem.Create("Water", "Beverages", [Variant(null, -1m)]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_AllowsAZeroPrice()
    {
        var item = MenuItem.Create("Complimentary Water", "Beverages", [Variant(null, 0m)]);

        item.Variants.Should().ContainSingle().Which.Price.Should().Be(0m);
    }

    [Fact]
    public void Create_WithMoreThanOneVariant_RequiresEveryOneToBeNamed()
    {
        var act = () => MenuItem.Create(
            "Chicken Fried Rice", "Rice & Curry", [Variant("Normal", 650m), Variant(null, 850m)]);

        act.Should().Throw<ArgumentException>("every size needs its own name once there is more than one");
    }

    [Fact]
    public void Create_RejectsDuplicateVariantNamesRegardlessOfCasing()
    {
        var act = () => MenuItem.Create(
            "Chicken Fried Rice", "Rice & Curry", [Variant("Full", 850m), Variant("FULL", 900m)]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateDetails_ChangesNameAndCategoryButLeavesVariantsAlone()
    {
        var item = MenuItem.Create("Fried Rice", "Rice", [Variant(null, 800m)]);

        item.UpdateDetails("Chicken Fried Rice", "Rice & Curry");

        item.Name.Should().Be("Chicken Fried Rice");
        item.Category.Should().Be("Rice & Curry");
        item.Variants.Should().ContainSingle().Which.Price.Should().Be(800m);
    }

    [Fact]
    public void ReplaceVariants_AddsUpdatesAndRemovesInOneCall()
    {
        var item = MenuItem.Create(
            "Chicken Fried Rice", "Rice & Curry", [Variant("Normal", 650m), Variant("Full", 850m)]);
        var normalId = item.Variants.Single(v => v.Name == "Normal").Id;

        // Normal gets repriced, Full disappears, Family is new.
        item.ReplaceVariants([
            new MenuItemVariantEdit(normalId, "Normal", 700m),
            new MenuItemVariantEdit(null, "Family", 1500m),
        ]);

        item.Variants.Should().HaveCount(2);
        item.Variants.Should().Contain(v => v.Id == normalId && v.Name == "Normal" && v.Price == 700m);
        item.Variants.Should().Contain(v => v.Name == "Family" && v.Price == 1500m);
        item.Variants.Should().NotContain(v => v.Name == "Full");
    }

    [Fact]
    public void ReplaceVariants_RejectsAnEmptyList()
    {
        var item = MenuItem.Create("Fried Rice", "Rice", [Variant(null, 800m)]);

        var act = () => item.ReplaceVariants([]);

        act.Should().Throw<ArgumentException>("a menu item must always keep at least one size");
    }

    [Fact]
    public void ActivateAndDeactivate_ToggleIsActive()
    {
        var item = MenuItem.Create("Fried Rice", "Rice", [Variant(null, 800m)]);

        item.Deactivate();
        item.IsActive.Should().BeFalse();

        item.Activate();
        item.IsActive.Should().BeTrue();
    }
}
