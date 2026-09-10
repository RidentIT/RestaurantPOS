namespace RestaurantPOS.API.Contracts.Users;

/// <summary>Adds a steward to the roster.</summary>
public sealed record CreateStewardRequest(string Name);

/// <summary>Corrects a steward's name.</summary>
public sealed record RenameStewardRequest(string Name);

/// <summary>Retires or reinstates a steward.</summary>
public sealed record SetStewardActiveRequest(bool IsActive);
