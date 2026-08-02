using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A grant of one module to one user. Access is currently all-or-nothing per module:
/// the presence of a row means the user may open that module.
/// </summary>
/// <remarks>
/// Modelled as its own row (rather than, say, a bit mask on <see cref="User"/>) so that
/// action-level flags such as <c>CanCreate</c> or <c>CanApprove</c> can later be added as
/// nullable columns without restructuring existing data.
/// </remarks>
public sealed class UserModulePermission
{
    // EF Core materialisation.
    private UserModulePermission()
    {
    }

    internal UserModulePermission(Guid userId, AppModule module)
    {
        UserId = userId;
        Module = module;
        GrantedAtUtc = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

    public AppModule Module { get; private set; }

    public DateTime GrantedAtUtc { get; private set; }
}