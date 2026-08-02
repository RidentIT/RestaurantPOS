using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projects <see cref="User"/> aggregates onto their read models.</summary>
public static class UserMappings
{
    public static UserDto ToDto(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            MustChangePassword = user.MustChangePassword,
            IsSystemAdmin = user.IsSystemAdmin,
            HasApprovalPin = user.HasApprovalPin,
            LastLoginAtUtc = user.LastLoginAtUtc,
            CreatedAtUtc = user.CreatedAtUtc,
            Modules = user.EffectiveModules(),
        };
    }
}