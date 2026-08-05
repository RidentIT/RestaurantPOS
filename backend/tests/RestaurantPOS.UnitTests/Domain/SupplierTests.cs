using FluentAssertions;

using RestaurantPOS.Domain.Entities;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class SupplierTests
{
    [Fact]
    public void Create_StartsActiveWithZeroPaymentTermsByDefault()
    {
        var supplier = Supplier.Create("ABC Wholesale");

        supplier.IsActive.Should().BeTrue();
        supplier.PaymentTermsDays.Should().Be(0);
        supplier.CreditLimit.Should().BeNull();
        supplier.LeadTimeDays.Should().BeNull();
    }

    [Fact]
    public void Create_StoresContactDetailsAndTerms()
    {
        var supplier = Supplier.Create(
            "ABC Wholesale",
            contactName: "Mr. Perera",
            phone: "0771234567",
            email: "  ABC@Wholesale.LK ",
            address: "123 Galle Rd",
            paymentTermsDays: 30,
            creditLimit: 100000m,
            leadTimeDays: 3);

        supplier.ContactName.Should().Be("Mr. Perera");
        supplier.Phone.Should().Be("0771234567");
        supplier.Email.Should().Be("abc@wholesale.lk", "email is normalised to lower case like elsewhere in the domain");
        supplier.PaymentTermsDays.Should().Be(30);
        supplier.CreditLimit.Should().Be(100000m);
        supplier.LeadTimeDays.Should().Be(3);
    }

    [Fact]
    public void Create_RejectsNegativePaymentTerms()
    {
        var act = () => Supplier.Create("ABC", paymentTermsDays: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_RejectsNegativeCreditLimit()
    {
        var act = () => Supplier.Create("ABC", creditLimit: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_RejectsNegativeLeadTime()
    {
        var act = () => Supplier.Create("ABC", leadTimeDays: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdateDetails_ReplacesEveryField()
    {
        var supplier = Supplier.Create("ABC Wholesale", paymentTermsDays: 30);

        supplier.UpdateDetails("XYZ Traders", "New Contact", "011", "new@x.lk", "New Address", 0, null, null);

        supplier.Name.Should().Be("XYZ Traders");
        supplier.PaymentTermsDays.Should().Be(0);
        supplier.CreditLimit.Should().BeNull();
    }

    [Fact]
    public void ActivateAndDeactivate_ToggleIsActive()
    {
        var supplier = Supplier.Create("ABC Wholesale");

        supplier.Deactivate();
        supplier.IsActive.Should().BeFalse();

        supplier.Activate();
        supplier.IsActive.Should().BeTrue();
    }
}