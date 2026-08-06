using FluentAssertions;

using RestaurantPOS.Application.Common.Mappings;
using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class KitchenTicketTests
{
    private static readonly DateTime Now = new(2026, 8, 5, 19, 45, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 8, 5);

    private static Order OpenOrder()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid());
        order.AddItems([new NewOrderItem(Guid.NewGuid(), "Fried Rice", 250m, 1, null)], Now);
        order.Confirm(1, Today, Now);

        return order;
    }

    [Fact]
    public void Advance_MovesForwardAndStampsTheTime()
    {
        var ticket = OpenOrder().Tickets.Single();

        ticket.Advance(KitchenTicketStatus.Preparing, Now);
        ticket.Advance(KitchenTicketStatus.Ready, Now.AddMinutes(8));
        ticket.Advance(KitchenTicketStatus.Served, Now.AddMinutes(10));

        ticket.Status.Should().Be(KitchenTicketStatus.Served);
        ticket.StartedAtUtc.Should().Be(Now);
        ticket.ReadyAtUtc.Should().Be(Now.AddMinutes(8));
        ticket.ServedAtUtc.Should().Be(Now.AddMinutes(10));
    }

    [Fact]
    public void Advance_CanSkipAheadWhenAPlateGoesStraightOut()
    {
        var ticket = OpenOrder().Tickets.Single();

        ticket.Advance(KitchenTicketStatus.Ready, Now);

        ticket.Status.Should().Be(KitchenTicketStatus.Ready);
        ticket.StartedAtUtc.Should().BeNull("it was never marked as started");
    }

    [Fact]
    public void Advance_RefusesToGoBackwards()
    {
        var ticket = OpenOrder().Tickets.Single();
        ticket.Advance(KitchenTicketStatus.Ready, Now);

        var act = () => ticket.Advance(KitchenTicketStatus.Preparing, Now);

        act.Should().Throw<InvalidOperationException>("prep timings are read back as kitchen performance");
    }

    [Fact]
    public void Advance_RefusesToRepeatTheCurrentStatus()
    {
        var ticket = OpenOrder().Tickets.Single();
        ticket.Advance(KitchenTicketStatus.Preparing, Now);

        var act = () => ticket.Advance(KitchenTicketStatus.Preparing, Now);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CancellationTickets_AreNotWorkable()
    {
        var order = OpenOrder();
        var ticket = order.RemoveItem(order.Items.Single().Id, Now);

        ticket!.IsWorkable.Should().BeFalse("a cancellation slip is a notice, not food to cook");
    }

    [Fact]
    public void DeriveKitchenStatus_ReportsTheLeastAdvancedTicket()
    {
        var order = OpenOrder();
        order.Tickets.Single().Advance(KitchenTicketStatus.Ready, Now);
        order.AddItems([new NewOrderItem(Guid.NewGuid(), "Sandwich", 150m, 1, null)], Now);

        var status = OrderMappings.DeriveKitchenStatus(order.Tickets);

        status.Should().Be(KitchenTicketStatus.New,
            "one dish is plated but another has only just been ordered, so the table is not ready");
    }

    [Fact]
    public void DeriveKitchenStatus_IgnoresCancellationSlips()
    {
        var order = OpenOrder();
        order.AddItems([new NewOrderItem(Guid.NewGuid(), "Sandwich", 150m, 1, null)], Now);

        foreach (var ticket in order.Tickets)
        {
            ticket.Advance(KitchenTicketStatus.Ready, Now);
        }

        order.RemoveItem(order.Items.First().Id, Now);

        OrderMappings.DeriveKitchenStatus(order.Tickets).Should().Be(KitchenTicketStatus.Ready,
            "voiding a line must not drag a plated table back to the start of the queue");
    }

    [Fact]
    public void DeriveKitchenStatus_IsNullBeforeAnythingReachesTheKitchen()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid());
        order.AddItems([new NewOrderItem(Guid.NewGuid(), "Fried Rice", 250m, 1, null)], Now);

        OrderMappings.DeriveKitchenStatus(order.Tickets).Should().BeNull();
    }
}
