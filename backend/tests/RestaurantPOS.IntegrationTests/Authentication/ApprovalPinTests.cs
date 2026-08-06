using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Authentication;

/// <summary>
/// Covers the approval PIN: the mechanism other modules will call when a member of staff needs
/// an administrator to authorise something at the till, such as cancelling an order.
/// </summary>
public class ApprovalPinTests : IntegrationTestBase
{
    [Fact]
    public async Task AdminCanGenerateAPin_AndItIsReturnedExactlyOnce()
    {
        var session = await SignInAsAdminAsync();
        session.User.HasApprovalPin.Should().BeFalse();

        var response = await Client.SetApprovalPinAsync(AdminPassword);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pin = (await PosApiClient.ReadAsync<PinResponse>(response)).Pin;
        pin.Should().HaveLength(4).And.MatchRegex("^[0-9]{4}$");

        // The PIN itself is never readable again — only the fact that one exists.
        var me = await PosApiClient.ReadAsync<UserResponse>(await Client.GetMeAsync());
        me.HasApprovalPin.Should().BeTrue();
    }

    [Fact]
    public async Task AdminCanChooseTheirOwnPin()
    {
        await SignInAsAdminAsync();

        var response = await Client.SetApprovalPinAsync(AdminPassword, "4821");

        (await PosApiClient.ReadAsync<PinResponse>(response)).Pin.Should().Be("4821");
    }

    [Fact]
    public async Task SettingAPinRequiresTheCurrentPassword()
    {
        await SignInAsAdminAsync();

        var response = await Client.SetApprovalPinAsync("WrongPassword1", "4821");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.PasswordMismatch");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("abcd")]
    public async Task APinMustBeFourDigits(string pin)
    {
        await SignInAsAdminAsync();

        (await Client.SetApprovalPinAsync(AdminPassword, pin))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StaffCanPresentAnAdminPinToAuthoriseAnAction()
    {
        var admin = await SignInAsAdminAsync();
        var pin = (await PosApiClient.ReadAsync<PinResponse>(
            await Client.SetApprovalPinAsync(AdminPassword, "4821"))).Pin;

        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        var response = await staff.VerifyApprovalPinAsync(pin, "Cancel order #1042");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var approval = await PosApiClient.ReadAsync<ApprovalResponse>(response);
        approval.ApprovedByUserId.Should().Be(admin.User.Id);
        approval.ApprovedByName.Should().Be(admin.User.FullName);
    }

    [Fact]
    public async Task AWrongPinIsRejected()
    {
        await SignInAsAdminAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");
        var (staff, _) = await CreateAndSignInStaffAsync();

        var response = await staff.VerifyApprovalPinAsync("1111");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.InvalidPin");
    }

    [Fact]
    public async Task VerifyingReportsClearlyWhenNoAdminHasConfiguredAPin()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync();

        var response = await staff.VerifyApprovalPinAsync("4821");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.NoAdminPinConfigured");
    }

    [Fact]
    public async Task StaffCannotHoldAPinOfTheirOwn()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync();

        (await staff.SetApprovalPinAsync("Ravi@2026x", "4821"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task APinCanBeCleared()
    {
        await SignInAsAdminAsync();
        await Client.SetApprovalPinAsync(AdminPassword, "4821");

        (await Client.ClearApprovalPinAsync()).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var me = await PosApiClient.ReadAsync<UserResponse>(await Client.GetMeAsync());
        me.HasApprovalPin.Should().BeFalse();

        // Clearing a PIN that is not set is reported rather than silently succeeding.
        (await Client.ClearApprovalPinAsync()).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DemotingAnAdministratorRemovesTheirApprovalPin()
    {
        await SignInAsAdminAsync();

        var created = await Client.CreateUserAsync("owner", "Owner@2026", "Admin", "Restaurant Owner");
        var owner = await PosApiClient.ReadAsync<UserResponse>(created);

        var ownerClient = NewClient();
        var login = await ownerClient.LoginAsync("owner", "Owner@2026");
        ownerClient.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(login)).AccessToken);
        var changed = await ownerClient.ChangePasswordAsync("Owner@2026", "Owner@2026New");
        ownerClient.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(changed)).AccessToken);
        await ownerClient.SetApprovalPinAsync("Owner@2026New", "7777");

        await Client.UpdateUserAsync(owner.Id, "Restaurant Owner", "User", ["PosBilling"]);

        var demoted = await PosApiClient.ReadAsync<UserResponse>(await Client.GetUserAsync(owner.Id));
        demoted.HasApprovalPin.Should().BeFalse("an approval PIN carries administrator authority");
    }
}