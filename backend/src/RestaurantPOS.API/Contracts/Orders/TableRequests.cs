namespace RestaurantPOS.API.Contracts.Orders;

public sealed record CreateTableRequest(string Number, int Seats = 0, string? Notes = null);

public sealed record UpdateTableRequest(string Number, int Seats, string? Notes);

public sealed record SetTableActiveRequest(bool IsActive);
