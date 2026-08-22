using FluentAssertions;

using RestaurantPOS.Domain.Entities;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class RestaurantSettingsTests
{
    private static RestaurantSettings NewSettings() =>
        RestaurantSettings.Create("Sri Lakshmi Family Restaurant", "Jaffna Road", "Anuradhapura", "0777273794");

    [Fact]
    public void Create_StartsWithSensibleDefaults()
    {
        var settings = NewSettings();

        settings.TaxRatePercent.Should().Be(0m, "the owner prices VAT into each dish rather than itemising it");
        settings.ServiceChargeRatePercent.Should().Be(0m);
        settings.ApprovalPinMaxAttempts.Should().Be(3);
        settings.ApprovalPinLockoutMinutes.Should().Be(5);
        settings.BackupRetentionCount.Should().Be(7);
        settings.ReceiptFooterMessage.Should().Be("Thank You! Come Again!");
    }

    [Fact]
    public void UpdateBillCharges_AcceptsRatesWithinZeroToOneHundred()
    {
        var settings = NewSettings();

        settings.UpdateBillCharges(12.5m, 10m);

        settings.TaxRatePercent.Should().Be(12.5m);
        settings.ServiceChargeRatePercent.Should().Be(10m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void UpdateBillCharges_RejectsAnOutOfRangeTaxRate(decimal invalid)
    {
        var settings = NewSettings();

        var act = () => settings.UpdateBillCharges(invalid, 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(11, 5)]
    [InlineData(3, 0)]
    [InlineData(3, 61)]
    public void UpdateApprovalPinPolicy_RejectsValuesOutsideTheAllowedRange(int attempts, int lockoutMinutes)
    {
        var settings = NewSettings();

        var act = () => settings.UpdateApprovalPinPolicy(attempts, lockoutMinutes);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateApprovalPinPolicy_AcceptsTheBoundaryValues()
    {
        var settings = NewSettings();

        settings.UpdateApprovalPinPolicy(10, 60);

        settings.ApprovalPinMaxAttempts.Should().Be(10);
        settings.ApprovalPinLockoutMinutes.Should().Be(60);
    }

    [Fact]
    public void UpdateProfile_TrimsAndPersistsEveryField()
    {
        var settings = NewSettings();

        settings.UpdateProfile("  New Name  ", " New Address ", "Suite 2", "Colombo", "0112345678", "logo.png");

        settings.Name.Should().Be("New Name");
        settings.AddressLine1.Should().Be("New Address");
        settings.AddressLine2.Should().Be("Suite 2");
        settings.City.Should().Be("Colombo");
        settings.Phone.Should().Be("0112345678");
        settings.LogoPath.Should().Be("logo.png");
    }

    [Fact]
    public void UpdateProfile_RejectsAnEmptyName()
    {
        var settings = NewSettings();

        var act = () => settings.UpdateProfile("   ", "Address", null, null, null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateBackupSettings_RejectsRetentionOutsideOneToSixty()
    {
        var settings = NewSettings();

        var act = () => settings.UpdateBackupSettings(null, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
