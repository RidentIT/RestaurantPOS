using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 8, 5, 19, 45, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 8, 5);

    private static Order NewOrder() => Order.Create(Guid.NewGuid(), Guid.NewGuid());

    private static NewOrderItem Dish(string name = "Fried Rice", decimal price = 250m, int quantity = 1) =>
        new(Guid.NewGuid(), name, price, quantity, null);

    private static Order OpenOrderWith(params NewOrderItem[] items)
    {
        var order = NewOrder();
        order.AddItems(items.Length == 0 ? [Dish()] : items, Now);
        order.Confirm(1, Today, Now);

        return order;
    }

    [Fact]
    public void Create_StartsAsADraftHoldingItsTable()
    {
        var order = NewOrder();

        order.Status.Should().Be(OrderStatus.Draft);
        order.OrderNumber.Should().BeNull("a draft has not been numbered yet");
        order.IsLive.Should().BeTrue("a draft already holds its table");
    }

    [Fact]
    public void AddItems_OnADraft_RaisesNoKitchenTicket()
    {
        var order = NewOrder();

        var ticket = order.AddItems([Dish()], Now);

        ticket.Should().BeNull("the kitchen has not been told about this order yet");
        order.Tickets.Should().BeEmpty();
        order.Subtotal.Should().Be(250m);
    }

    [Fact]
    public void Confirm_NumbersTheOrderAndPrintsTheFirstKot()
    {
        var order = NewOrder();
        order.AddItems([Dish("Fried Rice", 250m), Dish("Sandwich", 150m)], Now);

        var ticket = order.Confirm(7, Today, Now);

        order.Status.Should().Be(OrderStatus.Open);
        order.OrderNumber.Should().Be(7);
        order.OrderDate.Should().Be(Today);
        ticket.Kind.Should().Be(KitchenTicketKind.New);
        ticket.TicketNumber.Should().Be(1);
        ticket.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void Confirm_RejectsAnOrderWithNoItems()
    {
        var order = NewOrder();

        var act = () => order.Confirm(1, Today, Now);

        act.Should().Throw<InvalidOperationException>("an order must have at least one item (BR-POS-002)");
    }

    [Fact]
    public void Confirm_RejectsAnOrderThatIsNotADraft()
    {
        var order = OpenOrderWith();

        var act = () => order.Confirm(2, Today, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddItems_OnAnOpenOrder_PrintsAnAdditionKotWithOnlyTheNewItems()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));

        var ticket = order.AddItems([Dish("Sandwich", 150m)], Now);

        ticket.Should().NotBeNull();
        ticket!.Kind.Should().Be(KitchenTicketKind.Addition);
        ticket.TicketNumber.Should().Be(2);
        ticket.Lines.Should().ContainSingle()
            .Which.MenuItemName.Should().Be("Sandwich", "the kitchen is already cooking the rice");
        order.Total.Should().Be(400m);
    }

    [Fact]
    public void ChangeItemQuantity_OnAnOpenOrder_TellsTheKitchenWhatTheQuantityWas()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m, quantity: 1));
        var item = order.Items.Single();

        var ticket = order.ChangeItemQuantity(item.Id, 3, Now);

        ticket.Should().NotBeNull();
        ticket!.Kind.Should().Be(KitchenTicketKind.Modification);
        ticket.Lines.Single().Quantity.Should().Be(3);
        ticket.Lines.Single().Note.Should().Be("Was 1");
        order.Total.Should().Be(750m);
    }

    [Fact]
    public void ChangeItemQuantity_ToTheSameNumber_ChangesNothingAndPrintsNothing()
    {
        var order = OpenOrderWith(Dish(quantity: 2));
        var item = order.Items.Single();

        var ticket = order.ChangeItemQuantity(item.Id, 2, Now);

        ticket.Should().BeNull("nothing changed, so the kitchen has nothing to be told");
        order.Tickets.Should().ContainSingle();
    }

    [Fact]
    public void RemoveItem_OnADraft_DeletesTheLineOutright()
    {
        var order = NewOrder();
        order.AddItems([Dish()], Now);
        var item = order.Items.Single();

        var ticket = order.RemoveItem(item.Id, Now);

        ticket.Should().BeNull();
        order.Items.Should().BeEmpty("the kitchen never heard about it, so there is nothing to record");
    }

    [Fact]
    public void RemoveItem_OnAnOpenOrder_VoidsTheLineAndPrintsACancellationKot()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m), Dish("Sandwich", 150m));
        var sandwich = order.Items.Single(i => i.MenuItemName == "Sandwich");

        var ticket = order.RemoveItem(sandwich.Id, Now);

        ticket.Should().NotBeNull();
        ticket!.Kind.Should().Be(KitchenTicketKind.Cancellation);
        ticket.Lines.Single().Note.Should().Be("CANCELLED");

        order.Items.Should().HaveCount(2, "a voided line stays on the bill as a record");
        sandwich.IsCancelled.Should().BeTrue();
        sandwich.LineTotal.Should().Be(0m);
        order.Total.Should().Be(250m, "a voided line contributes nothing");
    }

    [Fact]
    public void SetDiscount_AsAPercentage_ComesOffTheSubtotal()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m, quantity: 4));

        order.SetDiscount(DiscountType.Percentage, 10m);

        order.Subtotal.Should().Be(1000m);
        order.DiscountAmount.Should().Be(100m);
        order.Total.Should().Be(900m);
    }

    [Fact]
    public void SetDiscount_Fixed_CannotExceedTheSubtotal()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));

        var act = () => order.SetDiscount(DiscountType.Fixed, 400m);

        act.Should().Throw<ArgumentOutOfRangeException>("a discount cannot exceed the subtotal (BR-POS-010)");
    }

    [Fact]
    public void DiscountAmount_IsCappedWhenTheBillShrinksBelowAFixedDiscount()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m), Dish("Sandwich", 150m));
        order.SetDiscount(DiscountType.Fixed, 400m);

        var sandwich = order.Items.Single(i => i.MenuItemName == "Sandwich");
        order.RemoveItem(sandwich.Id, Now);

        order.Subtotal.Should().Be(250m);
        order.DiscountAmount.Should().Be(250m, "the discount is capped by what is left on the bill");
        order.Total.Should().Be(0m, "and can never drive the bill negative");
    }

    [Fact]
    public void SetDiscount_RejectsAPercentageAboveOneHundred()
    {
        var order = OpenOrderWith();

        var act = () => order.SetDiscount(DiscountType.Percentage, 101m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_WithNoRatesConfigured_ChargesNeitherTaxNorServiceCharge()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));

        order.ServiceChargeAmount.Should().Be(0m);
        order.TaxAmount.Should().Be(0m);
        order.Total.Should().Be(250m, "BR-POS-011/BR-POS-012 default to nothing added");
    }

    [Fact]
    public void Total_StacksServiceChargeThenTaxOnTopOfTheDiscountedSubtotal()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid(), taxRatePercent: 10m, serviceChargeRatePercent: 5m);
        order.AddItems([Dish("Fried Rice", 1000m)], Now);
        order.Confirm(1, Today, Now);
        order.SetDiscount(DiscountType.Percentage, 10m);

        // Subtotal 1000, discount 100 -> 900. Service charge 5% of 900 = 45. Tax 10% of (900+45) = 94.5.
        order.ServiceChargeAmount.Should().Be(45m);
        order.TaxAmount.Should().Be(94.5m, "VAT is charged on the service charge too (local convention)");
        order.Total.Should().Be(1039.5m);
    }

    [Fact]
    public void Create_SnapshotsTheRatesAtCreationTime_SoLaterOrdersAreUnaffectedByEachOther()
    {
        var withRates = Order.Create(Guid.NewGuid(), Guid.NewGuid(), taxRatePercent: 10m, serviceChargeRatePercent: 0m);
        var withoutRates = Order.Create(Guid.NewGuid(), Guid.NewGuid());

        withRates.TaxRatePercent.Should().Be(10m);
        withoutRates.TaxRatePercent.Should().Be(0m, "each order only ever carries the rate it was created with");
    }

    [Fact]
    public void Complete_SettlesTheBillAndIssuesAReceipt()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));
        order.StartCheckout();
        order.AddPayment(OrderPaymentMethod.Cash, 250m, tenderedAmount: 500m, reference: null);

        var receipt = order.Complete("REC-001-2026", Now);

        order.Status.Should().Be(OrderStatus.Completed);
        order.IsLive.Should().BeFalse("the table is released once the bill is paid");
        order.ChangeDue.Should().Be(250m);
        receipt.Number.Should().Be("REC-001-2026");
    }

    [Fact]
    public void Complete_RejectsPaymentsThatDoNotAddUpToTheBill()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));
        order.StartCheckout();
        order.AddPayment(OrderPaymentMethod.Cash, 200m, null, null);

        var act = () => order.Complete("REC-001-2026", Now);

        act.Should().Throw<InvalidOperationException>("the tenders must equal the bill exactly (BR-POS-013)");
    }

    [Fact]
    public void Complete_AcceptsASplitAcrossSeveralMethods()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m), Dish("Sandwich", 150m));
        order.StartCheckout();
        order.AddPayment(OrderPaymentMethod.Cash, 100m, null, null);
        order.AddPayment(OrderPaymentMethod.Card, 300m, null, "AUTH-88");

        var act = () => order.Complete("REC-002-2026", Now);

        act.Should().NotThrow();
        order.AmountPaid.Should().Be(400m);
    }

    [Fact]
    public void AddPayment_IsRejectedBeforeCheckoutHasStarted()
    {
        var order = OpenOrderWith();

        var act = () => order.AddPayment(OrderPaymentMethod.Cash, 250m, null, null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReturnToOpen_DiscardsTendersKeyedAgainstTheOldBill()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));
        order.StartCheckout();
        order.AddPayment(OrderPaymentMethod.Cash, 250m, null, null);

        order.ReturnToOpen();

        order.Status.Should().Be(OrderStatus.Open);
        order.Payments.Should().BeEmpty("the bill they were counted against is about to change");
    }

    [Fact]
    public void AddItems_IsRejectedOnceCheckoutHasStarted()
    {
        var order = OpenOrderWith();
        order.StartCheckout();

        var act = () => order.AddItems([Dish("Sandwich", 150m)], Now);

        act.Should().Throw<InvalidOperationException>("the bill is frozen while the customer pays");
    }

    [Fact]
    public void Cancel_VoidsEveryLiveLineAndPrintsOneCancellationKot()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m), Dish("Sandwich", 150m));
        var approver = Guid.NewGuid();

        var ticket = order.Cancel(approver, Now);

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledByUserId.Should().Be(approver);
        order.Total.Should().Be(0m);
        order.IsLive.Should().BeFalse("a cancelled order releases its table");

        ticket.Should().NotBeNull();
        ticket!.Kind.Should().Be(KitchenTicketKind.Cancellation);
        ticket.Lines.Should().HaveCount(2);
        ticket.Lines.Should().OnlyContain(l => l.Note == "ORDER CANCELLED");
    }

    [Fact]
    public void Cancel_OnADraft_PrintsNothingBecauseTheKitchenNeverSawIt()
    {
        var order = NewOrder();
        order.AddItems([Dish()], Now);

        var ticket = order.Cancel(Guid.NewGuid(), Now);

        ticket.Should().BeNull();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_RejectsAnOrderThatIsAlreadyPaid()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));
        order.StartCheckout();
        order.AddPayment(OrderPaymentMethod.Cash, 250m, null, null);
        order.Complete("REC-001-2026", Now);

        var act = () => order.Cancel(Guid.NewGuid(), Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TicketNumbers_RunInSequenceAcrossEveryAmendment()
    {
        var order = OpenOrderWith(Dish("Fried Rice", 250m));
        order.AddItems([Dish("Sandwich", 150m)], Now);
        var item = order.Items.First();
        order.ChangeItemQuantity(item.Id, 2, Now);

        order.Tickets.Select(t => t.TicketNumber).Should().Equal(1, 2, 3);
        order.Tickets.Select(t => t.Kind).Should().Equal(
            KitchenTicketKind.New, KitchenTicketKind.Addition, KitchenTicketKind.Modification);
    }
}
