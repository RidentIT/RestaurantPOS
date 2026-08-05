using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class PurchaseOrderTests
{
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly Guid CreatedByUserId = Guid.NewGuid();
    private static readonly Guid RawMaterialA = Guid.NewGuid();
    private static readonly Guid RawMaterialB = Guid.NewGuid();

    private static PurchaseOrder NewOrder(params (Guid, decimal, decimal)[] lines) =>
        PurchaseOrder.Create(SupplierId, CreatedByUserId, lines.Length == 0 ? [(RawMaterialA, 10m, 200m)] : lines);

    [Fact]
    public void Create_StartsAsDraft()
    {
        var order = NewOrder();

        order.Status.Should().Be(PurchaseOrderStatus.Draft);
        order.SubmittedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_RequiresAtLeastOneLine()
    {
        var act = () => PurchaseOrder.Create(SupplierId, CreatedByUserId, []);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RejectsTheSameRawMaterialTwice()
    {
        var act = () => PurchaseOrder.Create(
            SupplierId, CreatedByUserId, [(RawMaterialA, 1m, 100m), (RawMaterialA, 2m, 100m)]);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_RejectsAZeroQuantity()
    {
        var act = () => PurchaseOrder.Create(SupplierId, CreatedByUserId, [(RawMaterialA, 0m, 100m)]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Create_RejectsANegativeUnitPrice()
    {
        var act = () => PurchaseOrder.Create(SupplierId, CreatedByUserId, [(RawMaterialA, 1m, -1m)]);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TotalAmount_SumsQuantityTimesUnitPriceAcrossLines()
    {
        var order = NewOrder((RawMaterialA, 10m, 200m), (RawMaterialB, 5m, 900m));

        order.TotalAmount.Should().Be(10m * 200m + 5m * 900m);
    }

    [Fact]
    public void Submit_SetsSubmittedAtAndMovesToSubmitted()
    {
        var order = NewOrder();
        var now = DateTime.UtcNow;

        order.Submit(now);

        order.Status.Should().Be(PurchaseOrderStatus.Submitted);
        order.SubmittedAtUtc.Should().Be(now);
    }

    [Fact]
    public void Submit_FailsWhenNotADraft()
    {
        var order = NewOrder();
        order.Submit(DateTime.UtcNow);

        var act = () => order.Submit(DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Confirm_RequiresSubmittedFirst()
    {
        var order = NewOrder();

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Confirm_MovesSubmittedToConfirmed()
    {
        var order = NewOrder();
        order.Submit(DateTime.UtcNow);

        order.Confirm();

        order.Status.Should().Be(PurchaseOrderStatus.Confirmed);
    }

    [Fact]
    public void ReplaceLines_OnlyWorksWhileDraft()
    {
        var order = NewOrder();
        order.Submit(DateTime.UtcNow);

        var act = () => order.ReplaceLines([(RawMaterialB, 1m, 1m)]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ReplaceLines_OverwritesRatherThanMerges()
    {
        var order = NewOrder((RawMaterialA, 1m, 100m));

        order.ReplaceLines([(RawMaterialB, 2m, 200m)]);

        order.Lines.Should().ContainSingle().Which.RawMaterialId.Should().Be(RawMaterialB);
    }

    [Fact]
    public void MarkDelivered_IsIdempotentSinceAnOrderCanBeReceivedAcrossMultipleGrns()
    {
        var order = NewOrder();
        order.Submit(DateTime.UtcNow);
        order.Confirm();

        order.MarkDelivered();
        var act = () => order.MarkDelivered();

        act.Should().NotThrow();
        order.Status.Should().Be(PurchaseOrderStatus.Delivered);
    }

    [Fact]
    public void MarkDelivered_RefusesACancelledOrder()
    {
        var order = NewOrder();
        order.Cancel();

        var act = () => order.MarkDelivered();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(PurchaseOrderStatus.Draft)]
    [InlineData(PurchaseOrderStatus.Submitted)]
    [InlineData(PurchaseOrderStatus.Confirmed)]
    public void Cancel_WorksFromAnyStateBeforeDelivery(PurchaseOrderStatus startingStatus)
    {
        var order = NewOrder();
        if (startingStatus >= PurchaseOrderStatus.Submitted) order.Submit(DateTime.UtcNow);
        if (startingStatus >= PurchaseOrderStatus.Confirmed) order.Confirm();

        order.Cancel();

        order.Status.Should().Be(PurchaseOrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_RefusesADeliveredOrder()
    {
        var order = NewOrder();
        order.Submit(DateTime.UtcNow);
        order.Confirm();
        order.MarkDelivered();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_RefusesAnAlreadyCancelledOrder()
    {
        var order = NewOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>();
    }
}