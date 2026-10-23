namespace RestaurantPOS.API.Contracts.Shifts;

public sealed record CheckInRequest(decimal OpeningCash);

public sealed record CheckOutRequest(decimal ClosingCash, string? Note);

public sealed record ForceCloseShiftRequest(decimal? ClosingCash, string? Note);
