using System.Security.Claims;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Security;

/// <summary>
/// Reads the acting user out of the current request's principal.
/// </summary>
/// <remarks>
/// Lives in the API project rather than infrastructure because it depends on
/// <see cref="IHttpContextAccessor"/>, which is a web concern.
/// </remarks>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Username => Principal?.FindFirstValue(ClaimTypes.Name);

    public bool IsAdmin => Principal?.IsInRole(nameof(UserRole.Admin)) ?? false;

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
}