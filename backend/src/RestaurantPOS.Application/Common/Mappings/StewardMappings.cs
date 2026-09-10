using RestaurantPOS.Application.Users.Dtos;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Application.Common.Mappings;

/// <summary>Projects <see cref="Steward"/> onto its read model.</summary>
public static class StewardMappings
{
    public static StewardDto ToDto(this Steward steward)
    {
        ArgumentNullException.ThrowIfNull(steward);

        return new StewardDto(steward.Id, steward.Name, steward.IsActive, steward.CreatedAtUtc);
    }
}
