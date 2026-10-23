using Xunit;

namespace RestaurantPOS.IntegrationTests.Common;

/// <summary>
/// Gives every test its own application instance and database.
/// </summary>
/// <remarks>
/// These tests deliberately mutate global state — the administrator's password, whether an
/// account is active, whether a PIN exists — so sharing one instance across a class would make
/// them order-dependent. xUnit constructs a fresh test class instance per test, so
/// <see cref="IAsyncLifetime"/> gives each one a clean install to work against.
/// </remarks>
public abstract class IntegrationTestBase : IAsyncLifetime, IDisposable
{
    /// <summary>Password the seeded administrator is moved to during provisioning.</summary>
    protected const string AdminPassword = "Lakshmi@2026";

    private CustomWebApplicationFactory _factory = null!;

    /// <summary>An unauthenticated client against this test's own application instance.</summary>
    protected PosApiClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        Client = new PosApiClient(_factory.CreateClient());

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory?.Dispose();
        }
    }

    /// <summary>Creates a second, independent client against the same application instance.</summary>
    protected PosApiClient NewClient() => new(_factory.CreateClient());

    /// <summary>
    /// Signs <see cref="Client"/> in as a fully provisioned administrator: seeded credentials
    /// exchanged for a real password, leaving an unrestricted session.
    /// </summary>
    protected async Task<SessionResponse> SignInAsAdminAsync()
    {
        var session = await Client.SignInAsProvisionedAdminAsync(AdminPassword);

        if (AutoCheckIn)
        {
            (await Client.CheckInAsync(0m)).EnsureSuccessStatusCode();
        }

        return session;
    }

    /// <summary>
    /// Every order needs an open shift, so signing in also checks the account in — otherwise each
    /// test that merely rings up a sale would have to repeat the same step. The shift tests
    /// themselves switch this off to exercise checking in and out directly.
    /// </summary>
    protected bool AutoCheckIn { get; set; } = true;

    /// <summary>
    /// Creates a staff account and signs a separate client in as it, completing the mandatory
    /// first password change. Returns that client and the account's id.
    /// </summary>
    protected async Task<(PosApiClient StaffClient, Guid UserId)> CreateAndSignInStaffAsync(
        string username = "cashier01",
        string finalPassword = "Ravi@2026x",
        params string[] modules)
    {
        const string temporaryPassword = "Temp@2026aa";

        var created = await Client.CreateUserAsync(
            username, temporaryPassword, "User", "Ravi Kumar", null, modules);

        created.EnsureSuccessStatusCode();
        var user = await PosApiClient.ReadAsync<UserResponse>(created);

        var staff = NewClient();
        var login = await staff.LoginAsync(username, temporaryPassword);
        login.EnsureSuccessStatusCode();
        staff.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(login)).AccessToken);

        var changed = await staff.ChangePasswordAsync(temporaryPassword, finalPassword);
        changed.EnsureSuccessStatusCode();
        staff.Authenticate((await PosApiClient.ReadAsync<SessionResponse>(changed)).AccessToken);

        if (AutoCheckIn && modules.Contains("PosBilling"))
        {
            (await staff.CheckInAsync(0m)).EnsureSuccessStatusCode();
        }

        return (staff, user.Id);
    }
}