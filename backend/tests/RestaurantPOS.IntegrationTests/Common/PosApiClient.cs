using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestaurantPOS.IntegrationTests.Common;

/// <summary>Session payload returned by sign-in, refresh and password change.</summary>
public sealed record SessionResponse(
    string AccessToken,
    string RefreshToken,
    UserResponse User);

/// <summary>A staff account as the API returns it.</summary>
public sealed record UserResponse(
    Guid Id,
    string Username,
    string FullName,
    string? Email,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    bool IsSystemAdmin,
    bool HasApprovalPin,
    IReadOnlyCollection<string> Modules);

/// <summary>A module catalog entry.</summary>
public sealed record ModuleResponse(string Module, string Name, string Group, bool AdminOnly);

/// <summary>Who authorised a PIN-gated action.</summary>
public sealed record ApprovalResponse(Guid ApprovedByUserId, string ApprovedByName);

/// <summary>The plaintext PIN, returned once when it is set.</summary>
public sealed record PinResponse(string Pin);

/// <summary>
/// Thin wrapper over <see cref="HttpClient"/> that keeps the tests focused on behaviour rather
/// than on URL and JSON plumbing.
/// </summary>
public sealed partial class PosApiClient(HttpClient http)
{
    internal const string BaseUrl = "/api/v1";

    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public HttpClient Http { get; } = http;

    /// <summary>Attaches a bearer token to every subsequent request, or clears it when null.</summary>
    public PosApiClient Authenticate(string? accessToken)
    {
        Http.DefaultRequestHeaders.Authorization = accessToken is null
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);

        return this;
    }

    public Task<HttpResponseMessage> LoginAsync(string username, string password) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/login", new { username, password }, Json);

    public Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/refresh", new { refreshToken }, Json);

    public Task<HttpResponseMessage> LogoutAsync(string? refreshToken) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/logout", new { refreshToken }, Json);

    public Task<HttpResponseMessage> ChangePasswordAsync(string currentPassword, string newPassword) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/change-password", new { currentPassword, newPassword }, Json);

    public Task<HttpResponseMessage> GetMeAsync() => Http.GetAsync($"{BaseUrl}/auth/me");

    public Task<HttpResponseMessage> GetModulesAsync() => Http.GetAsync($"{BaseUrl}/modules");

    public Task<HttpResponseMessage> GetUsersAsync(string? query = null) =>
        Http.GetAsync($"{BaseUrl}/users{query}");

    public Task<HttpResponseMessage> GetUserAsync(Guid id) => Http.GetAsync($"{BaseUrl}/users/{id}");

    public Task<HttpResponseMessage> CreateUserAsync(
        string username,
        string password,
        string role = "User",
        string fullName = "Test Staff",
        string? email = null,
        params string[] modules) =>
        Http.PostAsJsonAsync(
            $"{BaseUrl}/users",
            new { username, fullName, email, password, role, modules },
            Json);

    public Task<HttpResponseMessage> UpdateUserAsync(
        Guid id,
        string username,
        string fullName,
        string role,
        string[] modules,
        string? email = null) =>
        Http.PutAsJsonAsync($"{BaseUrl}/users/{id}", new { username, fullName, email, role, modules }, Json);

    public Task<HttpResponseMessage> SetUserActiveAsync(Guid id, bool isActive) =>
        Http.PutAsJsonAsync($"{BaseUrl}/users/{id}/status", new { isActive }, Json);

    public Task<HttpResponseMessage> ResetUserPasswordAsync(Guid id, string newPassword) =>
        Http.PostAsJsonAsync($"{BaseUrl}/users/{id}/password", new { newPassword }, Json);

    public Task<HttpResponseMessage> SetApprovalPinAsync(string currentPassword, string? pin = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/pin", new { currentPassword, pin }, Json);

    public Task<HttpResponseMessage> ClearApprovalPinAsync() => Http.DeleteAsync($"{BaseUrl}/auth/pin");

    public Task<HttpResponseMessage> VerifyApprovalPinAsync(string pin, string? reason = null) =>
        Http.PostAsJsonAsync($"{BaseUrl}/auth/pin/verify", new { pin, reason }, Json);

    /// <summary>
    /// Deserialises a response body, failing loudly if the call itself failed or the body is
    /// empty. Without the status check, a 4xx problem-details body would deserialise as a mostly
    /// null <typeparamref name="T"/> instead of an obvious failure — a genuine bug in the code
    /// under test would then read as a confusing assertion mismatch several lines away, instead of
    /// as the actual HTTP failure at the point it happened.
    /// </summary>
    public static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Expected a successful response but got {response.StatusCode}. Body: {body}");
        }

        var value = await response.Content.ReadFromJsonAsync<T>(Json);

        return value ?? throw new InvalidOperationException(
            $"Expected a {typeof(T).Name} body but the response was empty. Status: {response.StatusCode}.");
    }

    /// <summary>Reads the machine-readable <c>code</c> out of an RFC 7807 problem response.</summary>
    public static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(response);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.TryGetProperty("code", out var code)
            ? code.GetString()
            : null;
    }

    /// <summary>
    /// Signs in as the seeded administrator and completes the mandatory first password change,
    /// leaving the client authenticated with a fully privileged session.
    /// </summary>
    public async Task<SessionResponse> SignInAsProvisionedAdminAsync(string newPassword)
    {
        var login = await LoginAsync(
            CustomWebApplicationFactory.SeedAdminUsername,
            CustomWebApplicationFactory.SeedAdminPassword);

        login.EnsureSuccessStatusCode();
        Authenticate((await ReadAsync<SessionResponse>(login)).AccessToken);

        var changed = await ChangePasswordAsync(
            CustomWebApplicationFactory.SeedAdminPassword, newPassword);

        changed.EnsureSuccessStatusCode();
        var session = await ReadAsync<SessionResponse>(changed);
        Authenticate(session.AccessToken);

        return session;
    }
}