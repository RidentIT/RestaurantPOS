namespace RestaurantPOS.Application.Users.Dtos;

/// <summary>A steward as shown in the admin list and the order screen's picker.</summary>
public sealed record StewardDto(Guid Id, string Name, bool IsActive, DateTime CreatedAtUtc);
