using System.Text.RegularExpressions;

using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Enums;
using RestaurantPOS.Domain.Modules;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A staff account. Aggregate root owning the user's module grants and refresh tokens.
/// </summary>
/// <remarks>
/// The aggregate never sees plaintext secrets: callers pass in already-hashed passwords and
/// PINs. Hashing lives in the infrastructure layer behind <c>IPasswordHasher</c>.
/// </remarks>
public sealed partial class User : BaseEntity
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 32;
    public const int FullNameMaxLength = 120;
    public const int EmailMaxLength = 200;

    /// <summary>Length of the numeric approval PIN used to authorise privileged actions.</summary>
    public const int ApprovalPinLength = 4;

    private readonly List<UserModulePermission> _modulePermissions = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    // EF Core materialisation.
    private User()
    {
    }

    private User(
        string username,
        string fullName,
        string? email,
        string passwordHash,
        UserRole role,
        bool mustChangePassword,
        bool isSystemAdmin)
    {
        Username = NormaliseUsername(username);
        FullName = NormaliseFullName(fullName);
        Email = NormaliseEmail(email);
        PasswordHash = passwordHash;
        Role = role;
        MustChangePassword = mustChangePassword;
        IsSystemAdmin = isSystemAdmin;
        IsActive = true;
    }

    /// <summary>Login identifier. Always stored lower-cased so lookups are case-insensitive.</summary>
    public string Username { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Optional — floor staff are not required to have an email address.</summary>
    public string? Email { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Forces the password-reset screen on next sign-in.</summary>
    public bool MustChangePassword { get; private set; }

    /// <summary>Hash of the 4-digit approval PIN. Administrators only; null when unset.</summary>
    public string? ApprovalPinHash { get; private set; }

    public DateTime? ApprovalPinSetAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    /// <summary>
    /// True for the account created by database seeding. It cannot be deactivated or demoted,
    /// which guarantees the system can always be administered.
    /// </summary>
    public bool IsSystemAdmin { get; private set; }

    public IReadOnlyCollection<UserModulePermission> ModulePermissions => _modulePermissions.AsReadOnly();

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    /// <summary>Creates a staff account. Administrators implicitly hold every module.</summary>
    public static User Create(
        string username,
        string fullName,
        string? email,
        string passwordHash,
        UserRole role,
        IEnumerable<AppModule>? modules = null,
        bool mustChangePassword = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User(username, fullName, email, passwordHash, role, mustChangePassword, isSystemAdmin: false);

        if (role == UserRole.User && modules is not null)
        {
            user.ReplaceModuleGrants(modules);
        }

        return user;
    }

    /// <summary>Creates the built-in administrator used to bootstrap a fresh installation.</summary>
    public static User CreateSystemAdmin(string username, string fullName, string passwordHash) =>
        new(username, fullName, email: null, passwordHash, UserRole.Admin,
            mustChangePassword: true, isSystemAdmin: true);

    /// <summary>
    /// True when the user may open <paramref name="module"/>. Administrators always can.
    /// </summary>
    public bool HasAccessTo(AppModule module) =>
        Role == UserRole.Admin || _modulePermissions.Exists(p => p.Module == module);

    /// <summary>Modules the user may open, expanding an administrator to the full catalog.</summary>
    public IReadOnlyCollection<AppModule> EffectiveModules() =>
        Role == UserRole.Admin
            ? [.. ModuleCatalog.All.Select(d => d.Module)]
            : [.. _modulePermissions.Select(p => p.Module).Order()];

    public void UpdateProfile(string fullName, string? email)
    {
        FullName = NormaliseFullName(fullName);
        Email = NormaliseEmail(email);
    }

    /// <summary>Replaces the user's module grants wholesale. No-op for administrators.</summary>
    public void ReplaceModuleGrants(IEnumerable<AppModule> modules)
    {
        ArgumentNullException.ThrowIfNull(modules);

        _modulePermissions.Clear();

        if (Role == UserRole.Admin)
        {
            // Administrators derive access from their role, so explicit grants are redundant.
            return;
        }

        foreach (var module in modules.Distinct().Order())
        {
            _modulePermissions.Add(new UserModulePermission(Id, module));
        }
    }

    /// <summary>
    /// Changes the user's role. Promoting to administrator drops the now-redundant module
    /// grants; demoting leaves the user with no modules until an administrator assigns some.
    /// </summary>
    public void ChangeRole(UserRole role)
    {
        if (Role == role)
        {
            return;
        }

        Role = role;
        _modulePermissions.Clear();

        if (role == UserRole.User)
        {
            // A demoted administrator also loses the approval PIN, which is admin-only.
            ClearApprovalPin();
        }
    }

    /// <summary>Sets a new password chosen by the user, clearing the forced-reset flag.</summary>
    public void SetPassword(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        MustChangePassword = false;
    }

    /// <summary>
    /// Sets a password on the user's behalf (administrator reset). The user is forced to
    /// choose their own password at next sign-in.
    /// </summary>
    public void ResetPassword(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        MustChangePassword = true;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>Stores the hash of a newly issued approval PIN.</summary>
    public void SetApprovalPin(string pinHash, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pinHash);

        ApprovalPinHash = pinHash;
        ApprovalPinSetAtUtc = nowUtc;
    }

    public void ClearApprovalPin()
    {
        ApprovalPinHash = null;
        ApprovalPinSetAtUtc = null;
    }

    public bool HasApprovalPin => ApprovalPinHash is not null;

    public void RecordSuccessfulLogin(DateTime nowUtc) => LastLoginAtUtc = nowUtc;

    /// <summary>Records a newly issued refresh token and prunes ones that are no longer usable.</summary>
    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAtUtc, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        _refreshTokens.RemoveAll(t => !t.IsActive(nowUtc));

        var token = new RefreshToken(Id, tokenHash, expiresAtUtc, nowUtc);
        _refreshTokens.Add(token);
        return token;
    }

    /// <summary>Finds a usable refresh token by its hash.</summary>
    public RefreshToken? FindActiveRefreshToken(string tokenHash, DateTime nowUtc) =>
        _refreshTokens.Find(t => t.TokenHash == tokenHash && t.IsActive(nowUtc));

    public void RevokeRefreshToken(RefreshToken token, DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(token);
        token.Revoke(nowUtc);
    }

    /// <summary>Revokes every outstanding session, e.g. on sign-out or password change.</summary>
    public void RevokeAllRefreshTokens(DateTime nowUtc)
    {
        foreach (var token in _refreshTokens)
        {
            token.Revoke(nowUtc);
        }
    }

    /// <summary>
    /// True when <paramref name="username"/> is a syntactically valid login name. Applies the
    /// same trim-and-lower-case normalisation as <see cref="Create"/>, so anything this accepts
    /// the aggregate will too — otherwise validators would reject names the domain allows.
    /// </summary>
    public static bool IsValidUsername(string? username) =>
        !string.IsNullOrWhiteSpace(username) && UsernamePattern().IsMatch(username.Trim().ToLowerInvariant());

    private static string NormaliseUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var normalised = username.Trim().ToLowerInvariant();

        if (!UsernamePattern().IsMatch(normalised))
        {
            throw new ArgumentException(
                $"'{username}' is not a valid username. Use {UsernameMinLength}-{UsernameMaxLength} " +
                "letters, digits, dots, hyphens or underscores.",
                nameof(username));
        }

        return normalised;
    }

    private static string NormaliseFullName(string fullName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        var trimmed = fullName.Trim();

        return trimmed.Length > FullNameMaxLength
            ? throw new ArgumentException($"Full name cannot exceed {FullNameMaxLength} characters.", nameof(fullName))
            : trimmed;
    }

    private static string? NormaliseEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim().ToLowerInvariant();

        return trimmed.Length > EmailMaxLength
            ? throw new ArgumentException($"Email cannot exceed {EmailMaxLength} characters.", nameof(email))
            : trimmed;
    }

    // Attribute arguments must be compile-time literals, so the {3,32} bound is spelled out
    // here rather than interpolated. Keep it in step with UsernameMinLength/UsernameMaxLength.
    [GeneratedRegex("^[a-z0-9._-]{3,32}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernamePattern();
}