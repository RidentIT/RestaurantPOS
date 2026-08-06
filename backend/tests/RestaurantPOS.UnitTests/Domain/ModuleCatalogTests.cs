using FluentAssertions;

using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class ModuleCatalogTests
{
    [Fact]
    public void EveryDeclaredModuleIsDescribed()
    {
        var declared = Enum.GetValues<AppModule>();

        declared.Should().OnlyContain(m => ModuleCatalog.IsDefined(m),
            "the catalog drives navigation and the permission editor, so a module missing from " +
            "it would be unreachable in the UI");

        ModuleCatalog.All.Should().HaveCount(declared.Length);
    }

    [Fact]
    public void AdministrativeModulesAreNotIndividuallyGrantable()
    {
        ModuleCatalog.IsAssignableToUser(AppModule.UserManagement).Should().BeFalse();
        ModuleCatalog.IsAssignableToUser(AppModule.SystemSettings).Should().BeFalse();
        ModuleCatalog.IsAssignableToUser(AppModule.PosBilling).Should().BeTrue();
    }

    [Fact]
    public void AssignableExcludesExactlyTheAdminOnlyModules()
    {
        ModuleCatalog.Assignable.Should().BeEquivalentTo(ModuleCatalog.All.Where(d => !d.AdminOnly));
    }

    [Fact]
    public void ModulesAreOrderedAndCarryDisplayMetadata()
    {
        ModuleCatalog.All.Should().BeInAscendingOrder(d => d.SortOrder);
        ModuleCatalog.All.Should().OnlyContain(d =>
            !string.IsNullOrWhiteSpace(d.Name) &&
            !string.IsNullOrWhiteSpace(d.Group) &&
            !string.IsNullOrWhiteSpace(d.Description));
    }

    [Fact]
    public void EnumValuesAreStableBecauseTheyArePersisted()
    {
        // Renumbering these would silently repoint every stored permission at a different
        // module, so the expected values are pinned here deliberately.
        ((int)AppModule.PosBilling).Should().Be(1);
        ((int)AppModule.RecipeManagement).Should().Be(2);
        ((int)AppModule.StoreStockManagement).Should().Be(3);
        ((int)AppModule.KitchenStockRelease).Should().Be(4);
        ((int)AppModule.KitchenStockTracking).Should().Be(5);
        ((int)AppModule.KitchenOperations).Should().Be(6);
        ((int)AppModule.ReportsAnalytics).Should().Be(7);
        ((int)AppModule.UserManagement).Should().Be(8);
        ((int)AppModule.Notifications).Should().Be(9);
        ((int)AppModule.SupplierManagement).Should().Be(10);
        ((int)AppModule.ExpensesManagement).Should().Be(11);
        ((int)AppModule.SystemSettings).Should().Be(12);

        ((int)UserRole.Admin).Should().Be(1);
        ((int)UserRole.User).Should().Be(2);
    }
}