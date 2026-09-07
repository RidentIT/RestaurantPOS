using System.Net;

using FluentAssertions;

using RestaurantPOS.IntegrationTests.Common;

using Xunit;

namespace RestaurantPOS.IntegrationTests.Users;

public class UserManagementTests : IntegrationTestBase
{
    [Fact]
    public async Task Admin_CreatesAStaffAccountWithTheChosenModules()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateUserAsync(
            "cashier01", "Cashier@2026", "User", "Ravi Kumar", "Ravi@SriLakshmi.LK",
            "PosBilling", "KitchenOperations");

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var user = await PosApiClient.ReadAsync<UserResponse>(response);
        user.Username.Should().Be("cashier01");
        user.Email.Should().Be("ravi@srilakshmi.lk", "emails are normalised to lower case");
        user.Role.Should().Be("User");
        user.Modules.Should().BeEquivalentTo(["PosBilling", "KitchenOperations"]);
        user.MustChangePassword.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UsernamesAreUniqueRegardlessOfCasing()
    {
        await SignInAsAdminAsync();
        await Client.CreateUserAsync("cashier01", "Cashier@2026");

        var duplicate = await Client.CreateUserAsync("CASHIER01", "Another@2026");

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(duplicate)).Should().Be("User.UsernameTaken");
    }

    [Fact]
    public async Task AdministrativeModulesCannotBeGrantedToAStaffAccount()
    {
        await SignInAsAdminAsync();

        var response = await Client.CreateUserAsync(
            "sneaky", "Sneaky@2026", "User", "Test", null, "UserManagement");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task StaffAreConfinedToTheirGrantedModules()
    {
        await SignInAsAdminAsync();
        var (staff, _) = await CreateAndSignInStaffAsync(modules: "PosBilling");

        var me = await PosApiClient.ReadAsync<UserResponse>(await staff.GetMeAsync());
        me.Modules.Should().BeEquivalentTo(["PosBilling"]);

        // User administration is an admin power, not a grantable module.
        (await staff.GetUsersAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // The catalog itself stays readable, because the client renders its sidebar from it.
        (await staff.GetModulesAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdatingAUserReplacesItsModuleGrants()
    {
        await SignInAsAdminAsync();
        var created = await Client.CreateUserAsync(
            "cashier01", "Cashier@2026", "User", "Ravi Kumar", null, "PosBilling");
        var user = await PosApiClient.ReadAsync<UserResponse>(created);

        var updated = await Client.UpdateUserAsync(
            user.Id, user.Username, "Ravi K. Kumar", "User", ["ReportsAnalytics", "ExpensesManagement"]);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await PosApiClient.ReadAsync<UserResponse>(updated);
        result.FullName.Should().Be("Ravi K. Kumar");
        result.Modules.Should().BeEquivalentTo(["ReportsAnalytics", "ExpensesManagement"]);
    }

    [Fact]
    public async Task AnAdminCanRenameAUser()
    {
        await SignInAsAdminAsync();
        var created = await Client.CreateUserAsync(
            "cashier01", "Cashier@2026", "User", "Ravi Kumar", null, "PosBilling");
        var user = await PosApiClient.ReadAsync<UserResponse>(created);

        var updated = await Client.UpdateUserAsync(
            user.Id, "ravi.k", user.FullName, "User", ["PosBilling"]);

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await PosApiClient.ReadAsync<UserResponse>(updated)).Username.Should().Be("ravi.k");

        // The new name signs in; the old one no longer does.
        (await NewClient().LoginAsync("ravi.k", "Cashier@2026")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await NewClient().LoginAsync("cashier01", "Cashier@2026")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RenamingAUser_ToAnAlreadyTakenUsername_IsRejected()
    {
        await SignInAsAdminAsync();
        await Client.CreateUserAsync("cashier01", "Cashier@2026", "User", "Ravi Kumar", null, "PosBilling");
        var second = await PosApiClient.ReadAsync<UserResponse>(
            await Client.CreateUserAsync("cashier02", "Cashier@2026", "User", "Priya Fernando", null, "PosBilling"));

        var response = await Client.UpdateUserAsync(
            second.Id, "cashier01", second.FullName, "User", ["PosBilling"]);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("User.UsernameTaken");
    }

    [Fact]
    public async Task PromotingToAdmin_GrantsEveryModule()
    {
        await SignInAsAdminAsync();
        var created = await Client.CreateUserAsync(
            "manager", "Manager@2026", "User", "Priya", null, "PosBilling");
        var user = await PosApiClient.ReadAsync<UserResponse>(created);

        var promoted = await PosApiClient.ReadAsync<UserResponse>(
            await Client.UpdateUserAsync(user.Id, user.Username, "Priya", "Admin", []));

        var catalog = await PosApiClient.ReadAsync<List<ModuleResponse>>(await Client.GetModulesAsync());
        promoted.Role.Should().Be("Admin");
        promoted.Modules.Should().BeEquivalentTo(catalog.Select(m => m.Module));
    }

    [Fact]
    public async Task DeactivatingAUser_BlocksSignInAndKillsExistingSessions()
    {
        var admin = await SignInAsAdminAsync();
        var (staff, staffId) = await CreateAndSignInStaffAsync();

        var staffSession = await PosApiClient.ReadAsync<SessionResponse>(
            await NewClient().LoginAsync("cashier01", "Ravi@2026x"));

        (await Client.SetUserActiveAsync(staffId, false)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Outstanding sessions are revoked rather than left to expire on their own.
        (await staff.RefreshAsync(staffSession.RefreshToken))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var blockedLogin = await NewClient().LoginAsync("cashier01", "Ravi@2026x");
        blockedLogin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await PosApiClient.ReadErrorCodeAsync(blockedLogin)).Should().Be("Auth.AccountDeactivated");

        admin.User.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ReactivatingAUser_RestoresSignIn()
    {
        await SignInAsAdminAsync();
        var (_, staffId) = await CreateAndSignInStaffAsync();

        await Client.SetUserActiveAsync(staffId, false);
        await Client.SetUserActiveAsync(staffId, true);

        (await NewClient().LoginAsync("cashier01", "Ravi@2026x"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnAdminCannotDeactivateOrDemoteThemselves()
    {
        var session = await SignInAsAdminAsync();

        var deactivate = await Client.SetUserActiveAsync(session.User.Id, false);
        deactivate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(deactivate)).Should().Be("User.CannotDeactivateSelf");

        var demote = await Client.UpdateUserAsync(
            session.User.Id, session.User.Username, session.User.FullName, "User", []);
        demote.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(demote)).Should().Be("User.CannotDemoteSelf");
    }

    [Fact]
    public async Task TheBuiltInAdministratorCannotBeDeactivatedByAnotherAdmin()
    {
        var seeded = await SignInAsAdminAsync();

        // Promote a second administrator and sign in as them.
        var created = await Client.CreateUserAsync("owner", "Owner@2026", "Admin", "Restaurant Owner");
        var owner = await PosApiClient.ReadAsync<UserResponse>(created);

        var ownerClient = NewClient();
        var login = await ownerClient.LoginAsync("owner", "Owner@2026");
        ownerClient.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(login)).AccessToken);
        var changed = await ownerClient.ChangePasswordAsync("Owner@2026", "Owner@2026New");
        ownerClient.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(changed)).AccessToken);

        var response = await ownerClient.SetUserActiveAsync(seeded.User.Id, false);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await PosApiClient.ReadErrorCodeAsync(response)).Should().Be("User.CannotModifySystemAdmin");
        owner.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task ResettingAPassword_ForcesTheUserToChooseANewOneAndEndsTheirSessions()
    {
        await SignInAsAdminAsync();
        var (staff, staffId) = await CreateAndSignInStaffAsync();

        (await Client.ResetUserPasswordAsync(staffId, "Reset@2026x"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The old password no longer works.
        (await NewClient().LoginAsync("cashier01", "Ravi@2026x"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // The temporary one does, but lands the user straight back on the reset screen.
        var relogin = NewClient();
        var session = await PosApiClient.ReadAsync<SessionResponse>(
            await relogin.LoginAsync("cashier01", "Reset@2026x"));
        session.User.MustChangePassword.Should().BeTrue();

        relogin.Authenticate(session.AccessToken);
        (await relogin.GetModulesAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        staff.Should().NotBeNull();
    }

    [Fact]
    public async Task UsersCanBeFilteredBySearchRoleAndStatus()
    {
        await SignInAsAdminAsync();
        await Client.CreateUserAsync("cashier01", "Cashier@2026", "User", "Ravi Kumar");
        await Client.CreateUserAsync("chef01", "Chef@2026aa", "User", "Nimal Perera");

        var byName = await PosApiClient.ReadAsync<List<UserResponse>>(
            await Client.GetUsersAsync("?search=Nimal"));
        byName.Should().ContainSingle().Which.Username.Should().Be("chef01");

        var byUsername = await PosApiClient.ReadAsync<List<UserResponse>>(
            await Client.GetUsersAsync("?search=cashier"));
        byUsername.Should().ContainSingle().Which.Username.Should().Be("cashier01");

        var admins = await PosApiClient.ReadAsync<List<UserResponse>>(
            await Client.GetUsersAsync("?role=Admin"));
        admins.Should().OnlyContain(u => u.Role == "Admin");

        var active = await PosApiClient.ReadAsync<List<UserResponse>>(
            await Client.GetUsersAsync("?isActive=true"));
        active.Should().OnlyContain(u => u.IsActive);
    }

    [Fact]
    public async Task RequestingAnUnknownUserReturnsNotFound()
    {
        await SignInAsAdminAsync();

        var response = await Client.GetUserAsync(Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}