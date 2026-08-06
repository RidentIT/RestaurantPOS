using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Settings;

/// <summary>Receipt letterhead details, bound from the <c>Restaurant</c> configuration section.</summary>
public sealed class RestaurantProfileOptions : IRestaurantProfile
{
    public const string SectionName = "Restaurant";

    public string Name { get; set; } = "Sri Lakshmi Family Restaurant";

    public string AddressLine1 { get; set; } = "Jaffna Road, Sandamalgama";

    public string? AddressLine2 { get; set; }

    public string? City { get; set; } = "Anuradhapura";

    public string? Phone { get; set; } = "077 7273794";
}
