using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class RawMaterialTests
{
    [Fact]
    public void Create_StartsActiveWithNoThresholdsByDefault()
    {
        var rice = RawMaterial.Create("Rice", UnitOfMeasurement.Kilogram);

        rice.IsActive.Should().BeTrue();
        rice.MainStoreReorderLevel.Should().BeNull();
        rice.KitchenParLevel.Should().BeNull();
    }

    [Fact]
    public void Create_RejectsANegativeReorderLevel()
    {
        var act = () => RawMaterial.Create("Rice", UnitOfMeasurement.Kilogram, mainStoreReorderLevel: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_RejectsANegativeParLevel()
    {
        var act = () => RawMaterial.Create("Rice", UnitOfMeasurement.Kilogram, kitchenParLevel: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_CanChangeTheUnitOfMeasurement()
    {
        var rice = RawMaterial.Create("Rice", UnitOfMeasurement.Kilogram);

        rice.UpdateDetails("Rice", UnitOfMeasurement.Gram, null, null);

        rice.UnitOfMeasurement.Should().Be(UnitOfMeasurement.Gram);
    }

    [Fact]
    public void ActivateAndDeactivate_ToggleIsActive()
    {
        var rice = RawMaterial.Create("Rice", UnitOfMeasurement.Kilogram);

        rice.Deactivate();
        rice.IsActive.Should().BeFalse();

        rice.Activate();
        rice.IsActive.Should().BeTrue();
    }
}