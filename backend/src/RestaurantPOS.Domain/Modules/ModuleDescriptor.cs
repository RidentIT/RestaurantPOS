using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.Domain.Modules;

/// <summary>
/// Display metadata for an <see cref="AppModule"/>. Kept in the domain so the API, the
/// permission editor and the navigation sidebar all describe modules identically.
/// </summary>
/// <param name="Module">The module being described.</param>
/// <param name="Name">Human readable module name.</param>
/// <param name="Group">Grouping label used to nest related modules in the UI.</param>
/// <param name="Description">Short explanation of what the module allows.</param>
/// <param name="SortOrder">Stable ordering for menus and permission lists.</param>
/// <param name="AdminOnly">
/// When true the module carries administrative authority and is reserved for
/// <see cref="UserRole.Admin"/>; it is never offered in the per-user permission editor.
/// </param>
public sealed record ModuleDescriptor(
    AppModule Module,
    string Name,
    string Group,
    string Description,
    int SortOrder,
    bool AdminOnly = false);