using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Authentication;

public class AuthenticationTests : IntegrationTestBase
{
    [Fact]
    public async Task SeededAdmin_CanSignIn_ButIsFlaggedToChangeItsPassword()
    {
        var response = await Client.LoginAsync(
            CustomWebApplicationFactory.SeedAdminUsername,
            CustomWebApplicationFactory.SeedAdminPassword);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var session = await PosApiClient.ReadAsync<SessionResponse>(response);
        session.AccessToken.Should().NotBeNullOrWhiteSpace();
        session.RefreshToken.Should().NotBeNullOrWhiteSpace();
        session.User.MustChangePassword.Should().BeTrue();
        session.User.IsSystemAdmin.Should().BeTrue();
        session.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Administrators_HoldEveryModule()
    {
        var session = await SignInAsAdminAsync();

        var modules = await PosApiClient.ReadAsync<List<ModuleResponse>>(await Client.GetModulesAsync());

        session.User.Modules.Should().BeEquivalentTo(modules.Select(m => m.Module));
    }

    [Theory]
    [InlineData("admin", "WrongPassword1")]
    [InlineData("does-not-exist", "AnyPassword1")]
    public async Task BadCredentials_AreRejectedIndistinguishably(string username, string password)
    {
        var response = await Client.LoginAsync(username, password);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task PendingPasswordChange_ConfinesTheSessionToTheResetFlow()
    {
        var login = await Client.LoginAsync(
            CustomWebApplicationFactory.SeedAdminUsername,
            CustomWebApplicationFactory.SeedAdminPassword);

        Client.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(login)).AccessToken);

        // Ordinary work is refused with a code the client uses to route to the reset screen...
        var blocked = await Client.GetUsersAsync();
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PosApiClient.ReadErrorCodeAsync(blocked)).Should().Be("Auth.PasswordChangeRequired");

        // ...while the endpoints the reset screen itself needs stay reachable.
        (await Client.GetMeAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangingThePassword_LiftsTheRestriction()
    {
        var session = await SignInAsAdminAsync();

        session.User.MustChangePassword.Should().BeFalse();
        (await Client.GetUsersAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ChangePassword_RejectsAWrongCurrentPasswordAndAReusedOne()
    {
        await SignInAsAdminAsync();

        var wrongCurrent = await Client.ChangePasswordAsync("NotMyPassword1", "Another@2026");
        wrongCurrent.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(wrongCurrent)).Should().Be("Auth.PasswordMismatch");

        var reused = await Client.ChangePasswordAsync(AdminPassword, AdminPassword);
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PosApiClient.ReadErrorCodeAsync(reused)).Should().Be("Auth.PasswordReused");
    }

    [Fact]
    public async Task ChangePassword_EnforcesThePasswordPolicy()
    {
        await SignInAsAdminAsync();

        var response = await Client.ChangePasswordAsync(AdminPassword, "weak");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_IsRotatedAndTheOldOneStopsWorking()
    {
        var first = await SignInAsAdminAsync();

        var refreshed = await Client.RefreshAsync(first.RefreshToken);
        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await PosApiClient.ReadAsync<SessionResponse>(refreshed);
        second.RefreshToken.Should().NotBe(first.RefreshToken);

        // Replaying the consumed token must fail, so a stolen copy has a short useful life.
        (await Client.RefreshAsync(first.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Client.RefreshAsync(second.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_RejectsATokenItNeverIssued()
    {
        var response = await Client.RefreshAsync("not-a-real-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("Auth.InvalidRefreshToken");
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshTokenAndIsSafeToRepeat()
    {
        var session = await SignInAsAdminAsync();

        (await Client.LogoutAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.RefreshAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Signing out twice, or with nothing at all, must never fail.
        (await Client.LogoutAsync(session.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Client.LogoutAsync(null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangingPassword_EndsEveryOtherSession()
    {
        await SignInAsAdminAsync();

        // A second till signed in as the same account.
        var otherTill = NewClient();
        var otherLogin = await otherTill.LoginAsync(
            CustomWebApplicationFactory.SeedAdminUsername, AdminPassword);
        var otherSession = await PosApiClient.ReadAsync<SessionResponse>(otherLogin);

        await Client.ChangePasswordAsync(AdminPassword, "Rotated@2026");

        (await otherTill.RefreshAsync(otherSession.RefreshToken))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoints_RejectAnonymousCallers()
    {
        (await Client.GetMeAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Client.GetUsersAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Client.GetModulesAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}