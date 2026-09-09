using MediatR;

using RestaurantPOS.API.Contracts.Inventory;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Inventory.Commands.ConsumeStockForSale;
using RestaurantPOS.Application.Inventory.Commands.CreateGoodsReceivedNote;
using RestaurantPOS.Application.Inventory.Commands.CreateRawMaterial;
using RestaurantPOS.Application.Inventory.Commands.CreateStockAdjustment;
using RestaurantPOS.Application.Inventory.Commands.CreateStockRelease;
using RestaurantPOS.Application.Inventory.Commands.SetRawMaterialActive;
using RestaurantPOS.Application.Inventory.Commands.UpdateRawMaterial;
using RestaurantPOS.Application.Inventory.Queries.GetGoodsReceivedNoteById;
using RestaurantPOS.Application.Inventory.Queries.GetGoodsReceivedNotes;
using RestaurantPOS.Application.Inventory.Queries.GetRawMaterials;
using RestaurantPOS.Application.Inventory.Queries.GetStockLevels;
using RestaurantPOS.Application.Inventory.Queries.GetStockMovements;
using RestaurantPOS.Application.Inventory.Queries.GetStockReleaseById;
using RestaurantPOS.Application.Inventory.Queries.GetStockReleases;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Raw materials, and the two-store stock ledger (Main Store and Kitchen).</summary>
public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        MapRawMaterialEndpoints(routes);
        MapMainStoreEndpoints(routes);
        MapKitchenEndpoints(routes);
        MapReleaseEndpoints(routes);

        return routes;
    }

    private static void MapRawMaterialEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/raw-materials")
            .WithTags("Inventory")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.StoreStockManagement));

        group.MapGet("/", async (string? search, bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRawMaterialsQuery(search, isActive), ct);
                return result.ToHttpResult();
            })
            .WithName("GetRawMaterials")
            .WithSummary("Lists raw materials, optionally filtered.");

        group.MapPost("/", async (CreateRawMaterialRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateRawMaterialCommand(
                    request.Name, request.UnitOfMeasurement, request.MainStoreReorderLevel, request.KitchenParLevel);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(r => $"/api/v1/raw-materials/{r.Id}");
            })
            .WithName("CreateRawMaterial")
            .WithSummary("Creates a raw material.");

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateRawMaterialRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateRawMaterialCommand(
                    id, request.Name, request.UnitOfMeasurement, request.MainStoreReorderLevel, request.KitchenParLevel);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateRawMaterial")
            .WithSummary("Updates a raw material's details.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetRawMaterialActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetRawMaterialActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetRawMaterialActive")
            .WithSummary("Activates or deactivates a raw material.");
    }

    private static void MapMainStoreEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/inventory/main-store")
            .WithTags("Inventory")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.StoreStockManagement));

        group.MapGet("/stock", async (bool? lowStockOnly, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStockLevelsQuery(StoreType.MainStore, lowStockOnly ?? false), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMainStoreStock")
            .WithSummary("The current stock level of every active raw material in the Main Store.");

        group.MapGet("/movements", async (
                Guid? rawMaterialId, DateTime? from, DateTime? to, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetStockMovementsQuery(StoreType.MainStore, rawMaterialId, from, to), ct);
                return result.ToHttpResult();
            })
            .WithName("GetMainStoreMovements")
            .WithSummary("The Main Store's stock history, optionally filtered.");

        group.MapGet("/goods-received", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetGoodsReceivedNotesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithName("GetGoodsReceivedNotes")
            .WithSummary("Lists Goods Received Notes.");

        group.MapGet("/goods-received/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetGoodsReceivedNoteByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetGoodsReceivedNoteById")
            .WithSummary("Loads a single Goods Received Note with its lines.");

        group.MapPost("/goods-received", async (
                CreateGoodsReceivedNoteRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateGoodsReceivedNoteCommand(
                    request.SupplierId,
                    [.. request.Lines.Select(l => new GrnLineInput(l.RawMaterialId, l.Quantity))],
                    request.Notes,
                    request.PurchaseOrderId,
                    request.QualityRating,
                    request.HasIssue);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(n => $"/api/v1/inventory/main-store/goods-received/{n.Id}");
            })
            .WithName("CreateGoodsReceivedNote")
            .WithSummary("Records stock received from a supplier. Main Store stock increases immediately.");

        group.MapPost("/adjustments", async (
                CreateStockAdjustmentRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateStockAdjustmentCommand(
                    request.RawMaterialId, StoreType.MainStore, request.QuantityDelta, request.Reason);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("CreateMainStoreAdjustment")
            .WithSummary("Corrects a raw material's Main Store balance to match a physical count.");
    }

    private static void MapKitchenEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/inventory/kitchen")
            .WithTags("Inventory")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.KitchenStockTracking));

        group.MapGet("/stock", async (bool? lowStockOnly, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStockLevelsQuery(StoreType.Kitchen, lowStockOnly ?? false), ct);
                return result.ToHttpResult();
            })
            .WithName("GetKitchenStock")
            .WithSummary("The current stock level of every active raw material in the Kitchen.");

        group.MapGet("/movements", async (
                Guid? rawMaterialId, DateTime? from, DateTime? to, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetStockMovementsQuery(StoreType.Kitchen, rawMaterialId, from, to), ct);
                return result.ToHttpResult();
            })
            .WithName("GetKitchenMovements")
            .WithSummary("The Kitchen's stock history, optionally filtered.");

        group.MapPost("/adjustments", async (
                CreateStockAdjustmentRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateStockAdjustmentCommand(
                    request.RawMaterialId, StoreType.Kitchen, request.QuantityDelta, request.Reason);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("CreateKitchenAdjustment")
            .WithSummary("Corrects a raw material's Kitchen balance to match a physical count.");

        // Ahead of Point of Sale existing: the "complete order" flow will call this to deduct
        // Kitchen stock per the sold item's recipe. Kept under Kitchen Stock Tracking rather than
        // exposed to every module, since only the checkout flow is meant to call it.
        group.MapPost("/consumption/{menuItemVariantId:guid}", async (
                Guid menuItemVariantId, ConsumeStockRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ConsumeStockForSaleCommand(menuItemVariantId, request.QuantitySold), ct);
                return result.ToHttpResult();
            })
            .WithName("ConsumeStockForSale")
            .WithSummary("Deducts Kitchen stock per the menu item size's recipe for a completed sale.");
    }

    private static void MapReleaseEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/inventory/releases")
            .WithTags("Inventory")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.KitchenStockRelease));

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStockReleasesQuery(), ct);
                return result.ToHttpResult();
            })
            .WithName("GetStockReleases")
            .WithSummary("Lists Main Store → Kitchen stock releases.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetStockReleaseByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetStockReleaseById")
            .WithSummary("Loads a single stock release with its lines.");

        group.MapPost("/", async (CreateStockReleaseRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateStockReleaseCommand(
                    [.. request.Lines.Select(l => new StockReleaseLineInput(l.RawMaterialId, l.Quantity))],
                    request.Pin,
                    request.Notes);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(r => $"/api/v1/inventory/releases/{r.Id}");
            })
            .WithName("CreateStockRelease")
            .WithSummary("Releases stock from the Main Store to the Kitchen, authorised by an administrator's PIN.");
    }
}