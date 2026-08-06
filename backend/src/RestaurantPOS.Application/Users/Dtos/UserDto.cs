using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Application.Users.Dtos;

/// <summary>A staff account as presented to administrators in the user list and editor.</summary>
public sealed record UserDto
{
    public required Guid Id { get; init; }

    public required string Username { get; init; }

    public required string FullName { get; init; }

    public string? Email { get; init; }

    public required UserRole Role { get; init; }

    public required bool IsActive { get; init; }

    public required bool MustChangePassword { get; init; }

    /// <summary>True for the built-in administrator, which the UI protects from edits.</summary>
    public required bool IsSystemAdmin { get; init; }

    public required bool HasApprovalPin { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }

    public required DateTime CreatedAtUtc { get; init; }

    /// <summary>
    /// Modules the user can actually open. For administrators this is the whole catalog,
    /// even though no explicit grants are stored.
    /// </summary>
    public required IReadOnlyCollection<AppModule> Modules { get; init; }
}