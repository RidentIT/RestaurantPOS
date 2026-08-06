namespace RestaurantPOS.Application.Common.Security;

/// <summary>
/// Custom claim names carried by the access token. Shared so the issuer (infrastructure) and
/// the reader (API) cannot drift apart.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>One claim per module the user may open. Absent for administrators, who hold all.</summary>
    public const string Module = "module";

    /// <summary>
    /// Present and "true" while the user still owes a password change. The API refuses every
    /// endpoint except the password-change flow while this is set.
    /// </summary>
    public const string MustChangePassword = "must_change_password";
}