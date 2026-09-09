using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Users;

/// <summary>Creates a staff account.</summary>
/// <param name="Modules">Ignored for the Admin role, which holds every module.</param>
public sealed record CreateUserRequest(
    string Username,
    string FullName,
    string? Email,
    string Password,
    UserRole Role,
    IReadOnlyCollection<AppModule>? Modules);

/// <summary>Updates a staff account's profile, username, role and module grants.</summary>
public sealed record UpdateUserRequest(
    string Username,
    string FullName,
    string? Email,
    UserRole Role,
    IReadOnlyCollection<AppModule>? Modules);

/// <summary>Sets a temporary password that the user must then change.</summary>
public sealed record ResetUserPasswordRequest(string NewPassword);

/// <summary>Enables or disables a staff account.</summary>
public sealed record SetUserActiveRequest(bool IsActive);