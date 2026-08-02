namespace RestaurantPOS.API.Security;

/// <summary>
/// Marks an endpoint as reachable by a user who still owes a password change. Applied to the
/// handful of endpoints the reset screen itself needs.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowPendingPasswordChangeAttribute : Attribute;