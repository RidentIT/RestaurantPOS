using MediatR;

using RestaurantPOS.API.Contracts.Suppliers;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Suppliers.Commands.CancelPurchaseOrder;
using RestaurantPOS.Application.Suppliers.Commands.ConfirmPurchaseOrder;
using RestaurantPOS.Application.Suppliers.Commands.CreatePurchaseOrder;
using RestaurantPOS.Application.Suppliers.Commands.CreateSupplier;
using RestaurantPOS.Application.Suppliers.Commands.RecordSupplierPayment;
using RestaurantPOS.Application.Suppliers.Commands.SetSupplierActive;
using RestaurantPOS.Application.Suppliers.Commands.SetSupplierPrice;
using RestaurantPOS.Application.Suppliers.Commands.SubmitPurchaseOrder;
using RestaurantPOS.Application.Suppliers.Commands.UpdatePurchaseOrder;
using RestaurantPOS.Application.Suppliers.Commands.UpdateSupplier;
using RestaurantPOS.Application.Suppliers.Queries.GetPurchaseOrderById;
using RestaurantPOS.Application.Suppliers.Queries.GetPurchaseOrders;
using RestaurantPOS.Application.Suppliers.Queries.GetSupplierPayments;
using RestaurantPOS.Application.Suppliers.Queries.GetSupplierPerformance;
using RestaurantPOS.Application.Suppliers.Queries.GetSupplierPriceHistory;
using RestaurantPOS.Application.Suppliers.Queries.GetSupplierPrices;
using RestaurantPOS.Application.Suppliers.Queries.GetSuppliers;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Supplier master data, purchase orders, pricing, payments and performance.</summary>
public static class SupplierEndpoints
{
    public static IEndpointRouteBuilder MapSupplierEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // Reading the supplier list is also needed by Main Store staff picking a supplier on a
        // GRN, so it is mapped separately under the more permissive SupplierLookup policy rather
        // than the SupplierManagement-only group below.
        routes.MapGet("/suppliers", async (string? search, bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSuppliersQuery(search, isActive), ct);
                return result.ToHttpResult();
            })
            .WithTags("Suppliers")
            .RequireAuthorization(AuthorizationPolicies.SupplierLookup)
            .WithName("GetSuppliers")
            .WithSummary("Lists suppliers, optionally filtered.");

        var group = routes.MapGroup("/suppliers")
            .WithTags("Suppliers")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.SupplierManagement));

        MapSupplierCrud(group);
        MapPricing(group);
        MapPerformance(group);

        var orders = routes.MapGroup("/purchase-orders")
            .WithTags("Suppliers")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.SupplierManagement));

        MapPurchaseOrders(orders);
        MapPayments(orders);

        return routes;
    }

    private static void MapSupplierCrud(RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateSupplierRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreateSupplierCommand(
                    request.Name, request.ContactName, request.Phone, request.Email, request.Address,
                    request.PaymentTermsDays, request.CreditLimit, request.LeadTimeDays);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(s => $"/api/v1/suppliers/{s.Id}");
            })
            .WithName("CreateSupplier")
            .WithSummary("Creates a supplier.");

        group.MapPut("/{id:guid}", async (Guid id, UpdateSupplierRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateSupplierCommand(
                    id, request.Name, request.ContactName, request.Phone, request.Email, request.Address,
                    request.PaymentTermsDays, request.CreditLimit, request.LeadTimeDays);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateSupplier")
            .WithSummary("Updates a supplier's details, contact and terms.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetSupplierActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetSupplierActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetSupplierActive")
            .WithSummary("Activates or deactivates a supplier.");
    }

    private static void MapPricing(RouteGroupBuilder group)
    {
        group.MapGet("/prices", async (Guid? supplierId, Guid? rawMaterialId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSupplierPricesQuery(supplierId, rawMaterialId), ct);
                return result.ToHttpResult();
            })
            .WithName("GetSupplierPrices")
            .WithSummary("Current prices, filterable by supplier (a price list) or raw material (a comparison).");

        group.MapPut("/{id:guid}/prices", async (
                Guid id, SetSupplierPriceRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetSupplierPriceCommand(id, request.RawMaterialId, request.Price), ct);
                return result.ToHttpResult();
            })
            .WithName("SetSupplierPrice")
            .WithSummary("Records the current price a supplier charges for a raw material, keeping the change in history.");

        group.MapGet("/{id:guid}/prices/{rawMaterialId:guid}/history", async (
                Guid id, Guid rawMaterialId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSupplierPriceHistoryQuery(id, rawMaterialId), ct);
                return result.ToHttpResult();
            })
            .WithName("GetSupplierPriceHistory")
            .WithSummary("Every price a supplier has been recorded as charging for a raw material, newest first.");
    }

    private static void MapPerformance(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/performance", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSupplierPerformanceQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetSupplierPerformance")
            .WithSummary("On-time delivery, average lead time, quality rating and issue count for a supplier.");
    }

    private static void MapPurchaseOrders(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                Guid? supplierId, PurchaseOrderStatus? status, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPurchaseOrdersQuery(supplierId, status), ct);
                return result.ToHttpResult();
            })
            .WithName("GetPurchaseOrders")
            .WithSummary("Lists purchase orders, optionally filtered by supplier or status.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPurchaseOrderByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetPurchaseOrderById")
            .WithSummary("Loads a single purchase order with its lines and payment balance.");

        group.MapPost("/", async (CreatePurchaseOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CreatePurchaseOrderCommand(
                    request.SupplierId,
                    [.. request.Lines.Select(l => new PurchaseOrderLineInput(l.RawMaterialId, l.Quantity, l.UnitPrice))],
                    request.ExpectedDeliveryDate,
                    request.Notes);
                var result = await sender.Send(command, ct);
                return result.ToCreatedResult(o => $"/api/v1/purchase-orders/{o.Id}");
            })
            .WithName("CreatePurchaseOrder")
            .WithSummary("Creates a new purchase order in Draft.");

        group.MapPut("/{id:guid}", async (Guid id, UpdatePurchaseOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdatePurchaseOrderCommand(
                    id,
                    [.. request.Lines.Select(l => new UpdatePurchaseOrderLineInput(l.RawMaterialId, l.Quantity, l.UnitPrice))],
                    request.ExpectedDeliveryDate,
                    request.Notes);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdatePurchaseOrder")
            .WithSummary("Replaces a draft purchase order's lines and details.");

        group.MapPost("/{id:guid}/submit", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SubmitPurchaseOrderCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("SubmitPurchaseOrder")
            .WithSummary("Sends a draft purchase order to its supplier.");

        group.MapPost("/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmPurchaseOrderCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("ConfirmPurchaseOrder")
            .WithSummary("Records that the supplier has agreed to fulfil the order.");

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CancelPurchaseOrderCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("CancelPurchaseOrder")
            .WithSummary("Cancels a purchase order that has not yet been delivered.");
    }

    private static void MapPayments(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/payments", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSupplierPaymentsQuery(id, null), ct);
                return result.ToHttpResult();
            })
            .WithName("GetPurchaseOrderPayments")
            .WithSummary("Payments recorded against a purchase order.");

        group.MapPost("/{id:guid}/payments", async (
                Guid id, RecordSupplierPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new RecordSupplierPaymentCommand(
                    id, request.Amount, request.PaymentDateUtc, request.Method, request.InvoiceReference, request.Notes);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("RecordSupplierPayment")
            .WithSummary("Records a payment toward a purchase order.");
    }
}