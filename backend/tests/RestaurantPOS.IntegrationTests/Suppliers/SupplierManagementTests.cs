using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Suppliers;

public class SupplierManagementTests : IntegrationTestBase
{
    [Fact]
    public async Task CreatingASupplier_StoresContactDetailsAndTerms()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateSupplierAsync(
            "ABC Wholesale", "Mr. Perera", "0771234567", "ABC@Wholesale.LK", "123 Galle Rd",
            paymentTermsDays: 30, creditLimit: 100000m, leadTimeDays: 3);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var supplier = await PosApiClient.ReadAsync<SupplierResponse>(response);
        supplier.Email.Should().Be("abc@wholesale.lk");
        supplier.PaymentTermsDays.Should().Be(30);
        supplier.CreditLimit.Should().Be(100000m);
        supplier.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierNamesAreUniqueRegardlessOfCasing()
    {
        await SignInAsAdminAsync();
        await Client.CreateSupplierAsync("ABC Wholesale");

        var duplicate = await Client.CreateSupplierAsync("ABC WHOLESALE");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("Supplier.NameTaken");
    }

    [Fact]
    public async Task StaffWithOnlyStoreStockManagement_CanReadSuppliersForTheGrnDropdownButNotManageThem()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "StoreStockManagement");

        // Reading the list is needed to pick a supplier when recording a GRN, so it is allowed...
        (await staff.GetSuppliersAsync()).StatusCode.Should().Be(HttpStatusCode.OK);

        // ...but actually managing suppliers, pricing or purchase orders stays SupplierManagement-only.
        (await staff.CreateSupplierAsync("Sneaky Co")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.GetPurchaseOrdersAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task StaffGrantedSupplierManagement_CanManageSuppliers()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "SupplierManagement");

        (await staff.CreateSupplierAsync("ABC Wholesale")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ----- Purchase order lifecycle -----

    [Fact]
    public async Task CreatingAPurchaseOrder_StartsAsDraftWithComputedTotal()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, chickenId) = await SeedSupplierAndMaterialsAsync();

        var response = await Client.CreatePurchaseOrderAsync(
            supplierId, [(riceId, 50m, 210m), (chickenId, 20m, 950m)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(response);
        order.Status.Should().Be("Draft");
        order.TotalAmount.Should().Be(50m * 210m + 20m * 950m);
        order.Balance.Should().Be(order.TotalAmount);
    }

    [Fact]
    public async Task APurchaseOrder_RequiresAtLeastOneLine()
    {
        await SignInAsAdminAsync();
        var (supplierId, _, _) = await SeedSupplierAndMaterialsAsync();

        var response = await Client.CreatePurchaseOrderAsync(supplierId, []);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatingADraftOrder_ReplacesItsLines()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, chickenId) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        var updated = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.UpdatePurchaseOrderAsync(order.Id, [(chickenId, 5m, 900m)]));

        updated.Lines.Should().ContainSingle(l => l.RawMaterialId == chickenId);
        updated.TotalAmount.Should().Be(5m * 900m);
    }

    [Fact]
    public async Task OnceSubmitted_TheOrderCanNoLongerBeEdited()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        (await Client.SubmitPurchaseOrderAsync(order.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var editAttempt = await Client.UpdatePurchaseOrderAsync(order.Id, [(riceId, 99m, 1m)]);
        editAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(editAttempt)).Should().Be("PurchaseOrder.NotDraft");
    }

    [Fact]
    public async Task ConfirmingBeforeSubmitting_IsRejected()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        var response = await Client.ConfirmPurchaseOrderAsync(order.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("PurchaseOrder.NotConfirmable");
    }

    [Fact]
    public async Task FullLifecycle_DraftToSubmittedToConfirmed()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        await Client.SubmitPurchaseOrderAsync(order.Id);
        var confirmed = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.ConfirmPurchaseOrderAsync(order.Id));

        confirmed.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task CancellingADraftOrder_Succeeds()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        var cancelled = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CancelPurchaseOrderAsync(order.Id));

        cancelled.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task CancellingAnAlreadyDeliveredOrder_IsRejected()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var orderId = await CreateSubmittedConfirmedOrderAsync(supplierId, riceId);
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 10m)], purchaseOrderId: orderId);

        var response = await Client.CancelPurchaseOrderAsync(orderId);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("PurchaseOrder.NotCancellable");
    }

    // ----- GRN <-> PO reconciliation -----

    [Fact]
    public async Task RecordingAGrnAgainstAConfirmedOrder_MarksItDelivered()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var orderId = await CreateSubmittedConfirmedOrderAsync(supplierId, riceId);

        var grnResponse = await Client.CreateGoodsReceivedNoteAsync(
            supplierId, [(riceId, 10m)], purchaseOrderId: orderId, qualityRating: 4);

        var grn = await PosApiClient.ReadAsync<GoodsReceivedNoteResponse>(grnResponse);
        grn.PurchaseOrderId.Should().Be(orderId);
        grn.QualityRating.Should().Be(4);

        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(await Client.GetPurchaseOrderAsync(orderId));
        order.Status.Should().Be("Delivered");
    }

    [Fact]
    public async Task AGrn_CanStandAloneWithoutAPurchaseOrder()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();

        var response = await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 10m)]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await PosApiClient.ReadAsync<GoodsReceivedNoteResponse>(response)).PurchaseOrderId.Should().BeNull();
    }

    [Fact]
    public async Task AGrnAgainstACancelledOrder_IsRejected()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));
        await Client.CancelPurchaseOrderAsync(order.Id);

        var response = await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 10m)], purchaseOrderId: order.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("GoodsReceivedNote.PurchaseOrderCancelled");
    }

    [Fact]
    public async Task AGrnAgainstAnotherSuppliersOrder_IsRejected()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var otherSupplierId = (await PosApiClient.ReadAsync<SupplierResponse>(
            await Client.CreateSupplierAsync("XYZ Traders"))).Id;
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 200m)]));

        var response = await Client.CreateGoodsReceivedNoteAsync(
            otherSupplierId, [(riceId, 10m)], purchaseOrderId: order.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("GoodsReceivedNote.SupplierMismatch");
    }

    // ----- Payments -----

    [Fact]
    public async Task RecordingAPayment_ReducesTheOutstandingBalance()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 1000m)]));

        var response = await Client.RecordSupplierPaymentAsync(order.Id, 4000m, DateTime.UtcNow, "BankTransfer", "INV-001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await PosApiClient.ReadAsync<PurchaseOrderResponse>(await Client.GetPurchaseOrderAsync(order.Id));
        refreshed.AmountPaid.Should().Be(4000m);
        refreshed.Balance.Should().Be(6000m);
    }

    [Fact]
    public async Task APaymentExceedingTheOutstandingBalance_IsRejected()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 1000m)]));

        var response = await Client.RecordSupplierPaymentAsync(order.Id, 999999m, DateTime.UtcNow);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("SupplierPayment.ExceedsBalance");
    }

    [Fact]
    public async Task PaymentsListedForAnOrder_ReflectWhatWasRecorded()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(riceId, 10m, 1000m)]));
        await Client.RecordSupplierPaymentAsync(order.Id, 3000m, DateTime.UtcNow, "Cash");
        await Client.RecordSupplierPaymentAsync(order.Id, 2000m, DateTime.UtcNow, "Cheque");

        var payments = await PosApiClient.ReadAsync<List<SupplierPaymentResponse>>(
            await Client.GetPurchaseOrderPaymentsAsync(order.Id));

        payments.Should().HaveCount(2);
        payments.Sum(p => p.Amount).Should().Be(5000m);
    }

    // ----- Pricing -----

    [Fact]
    public async Task SettingAPrice_CanBeUpdatedAndKeepsHistory()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();

        await Client.SetSupplierPriceAsync(supplierId, riceId, 210m);
        var updated = await PosApiClient.ReadAsync<SupplierPriceResponse>(
            await Client.SetSupplierPriceAsync(supplierId, riceId, 215m));

        updated.Price.Should().Be(215m);

        var history = await PosApiClient.ReadAsync<List<SupplierPriceHistoryEntryResponse>>(
            await Client.GetSupplierPriceHistoryAsync(supplierId, riceId));

        history.Should().HaveCount(2);
        history.Select(h => h.Price).Should().Contain([210m, 215m]);
    }

    [Fact]
    public async Task PricesForARawMaterial_CanBeComparedAcrossSuppliers()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        var otherSupplierId = (await PosApiClient.ReadAsync<SupplierResponse>(
            await Client.CreateSupplierAsync("XYZ Traders"))).Id;

        await Client.SetSupplierPriceAsync(supplierId, riceId, 215m);
        await Client.SetSupplierPriceAsync(otherSupplierId, riceId, 205m);

        var prices = await PosApiClient.ReadAsync<List<SupplierPriceResponse>>(
            await Client.GetSupplierPricesAsync($"?rawMaterialId={riceId}"));

        prices.Should().HaveCount(2);
        prices.Should().Contain(p => p.SupplierId == supplierId && p.Price == 215m);
        prices.Should().Contain(p => p.SupplierId == otherSupplierId && p.Price == 205m);
    }

    // ----- Performance -----

    [Fact]
    public async Task Performance_ReflectsOnTimeDeliveryAndQualityRatings()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();

        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(
                supplierId, [(riceId, 10m, 200m)], expectedDeliveryDate: DateTime.UtcNow.AddDays(5)));
        await Client.SubmitPurchaseOrderAsync(order.Id);
        await Client.ConfirmPurchaseOrderAsync(order.Id);
        await Client.CreateGoodsReceivedNoteAsync(
            supplierId, [(riceId, 10m)], purchaseOrderId: order.Id, qualityRating: 5);

        var performance = await PosApiClient.ReadAsync<SupplierPerformanceResponse>(
            await Client.GetSupplierPerformanceAsync(supplierId));

        performance.TotalOrders.Should().Be(1);
        performance.DeliveredOrders.Should().Be(1);
        performance.OnTimeDeliveries.Should().Be(1);
        performance.OnTimeDeliveryRate.Should().Be(100);
        performance.AverageQualityRating.Should().Be(5);
        performance.IssueCount.Should().Be(0);
    }

    [Fact]
    public async Task Performance_CountsFlaggedIssues()
    {
        await SignInAsAdminAsync();
        var (supplierId, riceId, _) = await SeedSupplierAndMaterialsAsync();
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 10m)], hasIssue: true);
        await Client.CreateGoodsReceivedNoteAsync(supplierId, [(riceId, 5m)], hasIssue: false);

        var performance = await PosApiClient.ReadAsync<SupplierPerformanceResponse>(
            await Client.GetSupplierPerformanceAsync(supplierId));

        performance.IssueCount.Should().Be(1);
    }

    [Fact]
    public async Task Performance_ForASupplierWithNoOrders_ReportsNullsRatherThanErrors()
    {
        await SignInAsAdminAsync();
        var supplierId = (await PosApiClient.ReadAsync<SupplierResponse>(
            await Client.CreateSupplierAsync("Brand New Supplier"))).Id;

        var performance = await PosApiClient.ReadAsync<SupplierPerformanceResponse>(
            await Client.GetSupplierPerformanceAsync(supplierId));

        performance.TotalOrders.Should().Be(0);
        performance.OnTimeDeliveryRate.Should().BeNull();
        performance.AverageQualityRating.Should().BeNull();
    }

    // ----- Helpers -----

    private async Task<(Guid SupplierId, Guid RiceId, Guid ChickenId)> SeedSupplierAndMaterialsAsync()
    {
        var supplierId = (await PosApiClient.ReadAsync<SupplierResponse>(
            await Client.CreateSupplierAsync("ABC Wholesale"))).Id;
        var riceId = (await PosApiClient.ReadAsync<RawMaterialResponse>(
            await Client.CreateRawMaterialAsync("Rice"))).Id;
        var chickenId = (await PosApiClient.ReadAsync<RawMaterialResponse>(
            await Client.CreateRawMaterialAsync("Chicken"))).Id;

        return (supplierId, riceId, chickenId);
    }

    private async Task<Guid> CreateSubmittedConfirmedOrderAsync(Guid supplierId, Guid rawMaterialId)
    {
        var order = await PosApiClient.ReadAsync<PurchaseOrderResponse>(
            await Client.CreatePurchaseOrderAsync(supplierId, [(rawMaterialId, 10m, 200m)]));
        await Client.SubmitPurchaseOrderAsync(order.Id);
        await Client.ConfirmPurchaseOrderAsync(order.Id);

        return order.Id;
    }
}