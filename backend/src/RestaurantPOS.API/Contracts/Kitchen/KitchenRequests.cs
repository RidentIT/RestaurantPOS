using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Contracts.Kitchen;

public sealed record AdvanceKitchenTicketRequest(KitchenTicketStatus Status);
