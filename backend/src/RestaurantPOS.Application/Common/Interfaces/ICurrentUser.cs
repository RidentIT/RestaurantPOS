namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>Describes the user making the current request.</summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or null for anonymous requests.</summary>
    Guid? UserId { get; }

    string? Username { get; }

    bool IsAdmin { get; }
}