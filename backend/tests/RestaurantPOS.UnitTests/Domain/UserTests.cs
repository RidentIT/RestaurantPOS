using FluentAssertions;

using RestaurantPOS.Domain.Entities;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

using Xunit;

namespace RestaurantPOS.UnitTests.Domain;

public class UserTests
{
    private const string AnyHash = "hashed-password";

    private static User NewStaffUser(params AppModule[] modules) =>
        User.Create("cashier01", "Ravi Kumar", null, AnyHash, UserRole.User, modules);

    [Theory]
    [InlineData("Cashier01", "cashier01")]
    [InlineData("  ADMIN  ", "admin")]
    [InlineData("front.desk_2", "front.desk_2")]
    public void Create_NormalisesUsernameToLowerCase(string input, string expected)
    {
        var user = User.Create(input, "Someone", null, AnyHash, UserRole.User);

        user.Username.Should().Be(expected);
    }

    [Theory]
    [InlineData("ab")]                                   // shorter than the minimum
    [InlineData("has spaces")]
    [InlineData("bad!char")]
    [InlineData("thisusernameisfartoolongtobeacceptedbythedomain")]
    public void Create_RejectsMalformedUsernames(string username)
    {
        var act = () => User.Create(username, "Someone", null, AnyHash, UserRole.User);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NormalisesEmailAndTreatsBlankAsAbsent()
    {
        var withEmail = User.Create("a.user", "A User", "  Ravi@Example.COM ", AnyHash, UserRole.User);
        var withBlank = User.Create("b.user", "B User", "   ", AnyHash, UserRole.User);

        withEmail.Email.Should().Be("ravi@example.com");
        withBlank.Email.Should().BeNull();
    }

    [Fact]
    public void Create_FlagsNewAccountsForPasswordChange()
    {
        var user = NewStaffUser();

        user.MustChangePassword.Should().BeTrue();
        user.IsActive.Should().BeTrue();
        user.IsSystemAdmin.Should().BeFalse();
    }

    [Fact]
    public void HasAccessTo_IsLimitedToGrantedModulesForStaff()
    {
        var user = NewStaffUser(AppModule.PosBilling);

        user.HasAccessTo(AppModule.PosBilling).Should().BeTrue();
        user.HasAccessTo(AppModule.ReportsAnalytics).Should().BeFalse();
    }

    [Fact]
    public void HasAccessTo_IsUnconditionalForAdministrators()
    {
        var admin = User.Create("owner", "Owner", null, AnyHash, UserRole.Admin);

        admin.ModulePermissions.Should().BeEmpty("administrators derive access from their role");
        ModuleCatalog.All.Should().OnlyContain(d => admin.HasAccessTo(d.Module));
        admin.EffectiveModules().Should().HaveCount(ModuleCatalog.All.Count);
    }

    [Fact]
    public void Create_IgnoresModuleGrantsForAdministrators()
    {
        var admin = User.Create("owner", "Owner", null, AnyHash, UserRole.Admin, [AppModule.PosBilling]);

        admin.ModulePermissions.Should().BeEmpty();
    }

    [Fact]
    public void ReplaceModuleGrants_OverwritesAndDeduplicates()
    {
        var user = NewStaffUser(AppModule.PosBilling, AppModule.KitchenOperations);

        user.ReplaceModuleGrants([AppModule.ReportsAnalytics, AppModule.ReportsAnalytics]);

        user.EffectiveModules().Should().ContainSingle().Which.Should().Be(AppModule.ReportsAnalytics);
    }

    [Fact]
    public void ChangeRole_ToUser_DropsTheApprovalPin()
    {
        var admin = User.Create("owner", "Owner", null, AnyHash, UserRole.Admin);
        admin.SetApprovalPin("pin-hash", DateTime.UtcNow);

        admin.ChangeRole(UserRole.User);

        admin.HasApprovalPin.Should().BeFalse("an approval PIN is an administrator's authority");
        admin.EffectiveModules().Should().BeEmpty();
    }

    [Fact]
    public void ChangeRole_ToAdmin_ClearsNowRedundantGrants()
    {
        var user = NewStaffUser(AppModule.PosBilling);

        user.ChangeRole(UserRole.Admin);

        user.ModulePermissions.Should().BeEmpty();
        user.HasAccessTo(AppModule.SystemSettings).Should().BeTrue();
    }

    [Fact]
    public void SetPassword_ClearsTheForcedChangeFlag()
    {
        var user = NewStaffUser();

        user.SetPassword("new-hash");

        user.PasswordHash.Should().Be("new-hash");
        user.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public void ResetPassword_ForcesTheUserToChooseTheirOwn()
    {
        var user = NewStaffUser();
        user.SetPassword("chosen-by-user");

        user.ResetPassword("temporary-hash");

        user.PasswordHash.Should().Be("temporary-hash");
        user.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public void IssueRefreshToken_PrunesTokensThatAreNoLongerUsable()
    {
        var user = NewStaffUser();
        var now = DateTime.UtcNow;

        user.IssueRefreshToken("expired", now.AddMinutes(-1), now);
        user.IssueRefreshToken("live", now.AddDays(7), now);

        user.RefreshTokens.Should().ContainSingle().Which.TokenHash.Should().Be("live");
    }

    [Fact]
    public void FindActiveRefreshToken_IgnoresRevokedAndExpiredTokens()
    {
        var user = NewStaffUser();
        var now = DateTime.UtcNow;
        var token = user.IssueRefreshToken("abc", now.AddDays(7), now);

        user.FindActiveRefreshToken("abc", now).Should().NotBeNull();

        user.RevokeRefreshToken(token, now);

        user.FindActiveRefreshToken("abc", now).Should().BeNull();
        user.FindActiveRefreshToken("never-issued", now).Should().BeNull();
    }

    [Fact]
    public void RevokeAllRefreshTokens_EndsEverySession()
    {
        var user = NewStaffUser();
        var now = DateTime.UtcNow;
        user.IssueRefreshToken("a", now.AddDays(7), now);
        user.IssueRefreshToken("b", now.AddDays(7), now);

        user.RevokeAllRefreshTokens(now);

        user.RefreshTokens.Should().OnlyContain(t => !t.IsActive(now));
    }

    [Fact]
    public void CreateSystemAdmin_IsProtectedAndMustChangeItsPassword()
    {
        var admin = User.CreateSystemAdmin("admin", "System Administrator", AnyHash);

        admin.IsSystemAdmin.Should().BeTrue();
        admin.Role.Should().Be(UserRole.Admin);
        admin.MustChangePassword.Should().BeTrue();
    }
}