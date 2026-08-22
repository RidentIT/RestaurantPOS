using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Settings;

/// <summary>
/// Covers the business profile, bill charges, receipt, security and backup configuration — a
/// purely local, non-cloud module, so backups are the closest thing this restaurant has to a
/// safety net.
/// </summary>
public class SettingsTests : IntegrationTestBase
{
    private async Task<Guid> CreateTableAsync(string number)
    {
        var response = await Client.CreateTableAsync(number);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await PosApiClient.ReadAsync<TableResponse>(response)).Id;
    }

    private async Task<Guid> CreateMenuItemAsync(string name, decimal price)
    {
        var response = await Client.CreateMenuItemAsync(name, "Mains", price);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await PosApiClient.ReadAsync<MenuItemResponse>(response)).Id;
    }

    /// <summary>
    /// Points this test's backups at a folder of its own. Left on the default, every test in the
    /// run would share one <c>Backups</c> folder beside the test binary and trip over each other's
    /// files — a real backup folder does not have that problem because there is only one restaurant
    /// writing to it.
    /// </summary>
    private async Task<string> UseIsolatedBackupFolderAsync()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"restaurantpos-test-backups-{Guid.NewGuid():N}");
        (await Client.UpdateBackupSettingsAsync(folder, retentionCount: 7)).EnsureSuccessStatusCode();

        return folder;
    }

    [Fact]
    public async Task GetSettings_ReturnsTheSeededDefaults()
    {
        await SignInAsAdminAsync();

        var response = await Client.GetRestaurantSettingsAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await PosApiClient.ReadAsync<RestaurantSettingsResponse>(response);
        settings.Name.Should().Be("Sri Lakshmi Family Restaurant");
        settings.TaxRatePercent.Should().Be(0m);
        settings.ServiceChargeRatePercent.Should().Be(0m);
        settings.ApprovalPinMaxAttempts.Should().Be(3);
        settings.BackupRetentionCount.Should().Be(7);
    }

    [Fact]
    public async Task NonAdminCannotReadOrChangeSettings()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync();

        (await staff.GetRestaurantSettingsAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await staff.UpdateBillChargesAsync(5m, 5m)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateBusinessProfile_PersistsEveryField()
    {
        await SignInAsAdminAsync();

        var response = await Client.UpdateBusinessProfileAsync(
            "New Name Restaurant", "New Address", "Suite 2", "Colombo", "0112345678", "logo.png");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await PosApiClient.ReadAsync<RestaurantSettingsResponse>(response);
        updated.Name.Should().Be("New Name Restaurant");
        updated.City.Should().Be("Colombo");

        var reloaded = await PosApiClient.ReadAsync<RestaurantSettingsResponse>(
            await Client.GetRestaurantSettingsAsync());
        reloaded.Name.Should().Be("New Name Restaurant");
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(101, 0)]
    [InlineData(0, 101)]
    public async Task UpdateBillCharges_RejectsRatesOutsideZeroToOneHundred(decimal tax, decimal serviceCharge)
    {
        await SignInAsAdminAsync();

        var response = await Client.UpdateBillChargesAsync(tax, serviceCharge);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateBillCharges_AppliesOnlyToOrdersCreatedAfterwards()
    {
        await SignInAsAdminAsync();
        var table1 = await CreateTableAsync("1");
        var table2 = await CreateTableAsync("2");
        var dish = await CreateMenuItemAsync("Fried Rice", 1000m);

        // Order A starts life under the restaurant's original zero rates.
        var orderA = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(table1));

        (await Client.UpdateBillChargesAsync(10m, 5m)).EnsureSuccessStatusCode();

        // Order B is created after the rate change, so it should carry the new rates.
        var orderB = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(table2));

        await Client.AddOrderItemsAsync(orderA.Id, (dish, 1, null));
        await Client.AddOrderItemsAsync(orderB.Id, (dish, 1, null));

        var reloadedA = await PosApiClient.ReadAsync<OrderResponse>(await Client.GetOrderAsync(orderA.Id));
        var reloadedB = await PosApiClient.ReadAsync<OrderResponse>(await Client.GetOrderAsync(orderB.Id));

        reloadedA.TaxAmount.Should().Be(0m, "an order already in progress must not change under it");
        reloadedA.ServiceChargeAmount.Should().Be(0m);
        reloadedA.Total.Should().Be(1000m);

        reloadedB.ServiceChargeAmount.Should().Be(50m, "5% service charge on a 1000 subtotal");
        reloadedB.TaxAmount.Should().Be(105m, "10% VAT on the subtotal plus the service charge");
        reloadedB.Total.Should().Be(1155m);
    }

    [Fact]
    public async Task CompletingAnOrder_ProducesAReceiptWithTheConfiguredChargesAndFooter()
    {
        await SignInAsAdminAsync();
        var table = await CreateTableAsync("5");
        var dish = await CreateMenuItemAsync("Fried Rice", 1000m);

        (await Client.UpdateBillChargesAsync(10m, 5m)).EnsureSuccessStatusCode();
        (await Client.UpdateReceiptFooterAsync("Vibe Check Passed!")).EnsureSuccessStatusCode();

        var order = await PosApiClient.ReadAsync<OrderResponse>(await Client.CreateOrderAsync(table));
        await Client.AddOrderItemsAsync(order.Id, (dish, 1, null));
        await Client.ConfirmOrderAsync(order.Id);
        await Client.StartCheckoutAsync(order.Id);

        var paid = await Client.PayOrderAsync(order.Id, ("Cash", 1155m, 1155m));
        paid.EnsureSuccessStatusCode();

        var receipt = await PosApiClient.ReadAsync<ReceiptDocumentResponse>(paid);
        receipt.ServiceChargeAmount.Should().Be(50m);
        receipt.TaxAmount.Should().Be(105m);
        receipt.Total.Should().Be(1155m);
        receipt.FooterMessage.Should().Be("Vibe Check Passed!");
    }

    [Fact]
    public async Task UpdateApprovalPinPolicy_ChangesHowManyAttemptsATerminalGetsBeforeLockout()
    {
        await SignInAsAdminAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");
        await Client.UpdateApprovalPinPolicyAsync(maxAttempts: 1, lockoutMinutes: 1);

        var (staff, _) = await CreateAndSignInStaffAsync();

        var response = await staff.VerifyApprovalPinAsync("1111");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should()
            .Be("Auth.PinAttemptsExhausted", "one allowed attempt was configured, and it was just used");
    }

    [Fact]
    public async Task Backup_CanBeCreatedAndThenAppearsInTheList()
    {
        await SignInAsAdminAsync();
        var folder = await UseIsolatedBackupFolderAsync();

        try
        {
            var created = await Client.CreateBackupAsync();
            created.StatusCode.Should().Be(HttpStatusCode.OK);
            var backup = await PosApiClient.ReadAsync<BackupResponse>(created);
            backup.FileName.Should().StartWith("Backup-").And.EndWith(".zip");
            backup.SizeBytes.Should().BeGreaterThan(0);

            var list = await PosApiClient.ReadAsync<List<BackupResponse>>(await Client.GetBackupsAsync());
            list.Should().Contain(b => b.FileName == backup.FileName);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task RunDailyBackup_OnlyTakesOneBackupPerDay()
    {
        await SignInAsAdminAsync();
        var folder = await UseIsolatedBackupFolderAsync();

        try
        {
            var first = await PosApiClient.ReadAsync<bool>(await Client.RunDailyBackupAsync());
            first.Should().BeTrue("nothing has been backed up yet today");

            var second = await PosApiClient.ReadAsync<bool>(await Client.RunDailyBackupAsync());
            second.Should().BeFalse("today's backup already exists");

            var list = await PosApiClient.ReadAsync<List<BackupResponse>>(await Client.GetBackupsAsync());
            list.Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreBackup_RejectsAnyConfirmationTextOtherThanTheExactWord()
    {
        await SignInAsAdminAsync();
        var folder = await UseIsolatedBackupFolderAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");

        try
        {
            var backup = await PosApiClient.ReadAsync<BackupResponse>(await Client.CreateBackupAsync());

            var response = await Client.RestoreBackupAsync(backup.FileName, "4821", "restore");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Backup.RestoreConfirmationMismatch");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreBackup_RejectsAWrongPin()
    {
        await SignInAsAdminAsync();
        var folder = await UseIsolatedBackupFolderAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");

        try
        {
            var backup = await PosApiClient.ReadAsync<BackupResponse>(await Client.CreateBackupAsync());

            var response = await Client.RestoreBackupAsync(backup.FileName, "1111", "RESTORE");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.InvalidPin");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task RestoreBackup_RejectsAFileThatDoesNotExist()
    {
        await SignInAsAdminAsync();
        var folder = await UseIsolatedBackupFolderAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");

        try
        {
            var response = await Client.RestoreBackupAsync("Backup-does-not-exist.zip", "4821", "RESTORE");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
