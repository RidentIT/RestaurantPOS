using FluentAssertions;

using RestaurantPOS.Domain.Entities;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class RecipeTests
{
    private static readonly Guid MenuItemId = Guid.NewGuid();
    private static readonly Guid RawMaterialA = Guid.NewGuid();
    private static readonly Guid RawMaterialB = Guid.NewGuid();

    [Fact]
    public void Create_RequiresAtLeastOneLine()
    {
        var act = () => Recipe.Create(MenuItemId, []);

        act.Should().Throw<ArgumentException>("BR-REC-002: a recipe must contain at least one raw material");
    }

    [Fact]
    public void Create_RejectsAZeroOrNegativeQuantity()
    {
        var act = () => Recipe.Create(MenuItemId, [(RawMaterialA, 0m)]);

        act.Should().Throw<ArgumentOutOfRangeException>("BR-REC-003: quantities must be greater than zero");
    }

    [Fact]
    public void Create_AcceptsAFractionalQuantity()
    {
        var recipe = Recipe.Create(MenuItemId, [(RawMaterialA, 0.25m)]);

        recipe.Lines.Should().ContainSingle().Which.Quantity.Should().Be(0.25m);
    }

    [Fact]
    public void Create_RejectsTheSameRawMaterialTwice()
    {
        var act = () => Recipe.Create(MenuItemId, [(RawMaterialA, 1m), (RawMaterialA, 2m)]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_StartsEnabled()
    {
        var recipe = Recipe.Create(MenuItemId, [(RawMaterialA, 1m)]);

        recipe.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void ReplaceLines_OverwritesRatherThanMerges()
    {
        var recipe = Recipe.Create(MenuItemId, [(RawMaterialA, 1m)]);

        recipe.ReplaceLines([(RawMaterialB, 2m)]);

        recipe.Lines.Should().ContainSingle().Which.RawMaterialId.Should().Be(RawMaterialB);
    }

    [Fact]
    public void ReplaceLines_StillEnforcesAtLeastOneLine()
    {
        var recipe = Recipe.Create(MenuItemId, [(RawMaterialA, 1m)]);

        var act = () => recipe.ReplaceLines([]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnableAndDisable_ToggleIsEnabled()
    {
        var recipe = Recipe.Create(MenuItemId, [(RawMaterialA, 1m)]);

        recipe.Disable();
        recipe.IsEnabled.Should().BeFalse();

        recipe.Enable();
        recipe.IsEnabled.Should().BeTrue();
    }
}