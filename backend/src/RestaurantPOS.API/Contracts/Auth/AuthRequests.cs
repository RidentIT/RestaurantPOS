using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.API.Contracts.Auth;

/// <summary>Sign-in credentials.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>Exchanges a refresh token for a new session.</summary>
public sealed record RefreshRequest(string RefreshToken);

/// <summary>Ends a session. The token is optional so sign-out never fails.</summary>
public sealed record LogoutRequest(string? RefreshToken);

/// <summary>Changes the signed-in user's own password.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>Updates the signed-in user's own display name and email.</summary>
public sealed record UpdateProfileRequest(string FullName, string? Email);

/// <summary>
/// Sets the signed-in administrator's approval PIN. Leave <paramref name="Pin"/> null to have
/// the server generate a random <see cref="User.ApprovalPinLength"/>-digit PIN.
/// </summary>
public sealed record SetApprovalPinRequest(string CurrentPassword, string? Pin);

/// <summary>Presents a PIN for authorisation of a privileged action.</summary>
public sealed record VerifyApprovalPinRequest(string Pin, string? Reason);