namespace RestaurantPOS.Domain.Common;

#pragma warning disable CA1716
public record Error(string Code, string Description)
#pragma warning restore CA1716
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided.");

    public static Error Failure(string code, string description) => new(code, description);
    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Validation(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
}
