namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Coarse-grained role. Fine-grained access is driven by per-user module grants;
/// see <see cref="AppModule"/> and <c>UserModulePermission</c>.
/// </summary>
public enum UserRole
{
    /// <summary>Full access to every module regardless of explicit grants.</summary>
    Admin = 1,

    /// <summary>Access limited to the modules explicitly granted by an administrator.</summary>
    User = 2,
}