using MediatR;

using RestaurantPOS.API.Contracts.Orders;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Orders.Commands.AddOrderItems;
using RestaurantPOS.Application.Orders.Commands.CancelOrder;
using RestaurantPOS.Application.Orders.Commands.ChangeOrderItemQuantity;
using RestaurantPOS.Application.Orders.Commands.CompleteOrderPayment;
using RestaurantPOS.Application.Orders.Commands.ConfirmOrder;
using RestaurantPOS.Application.Orders.Commands.CreateOrder;
using RestaurantPOS.Application.Orders.Commands.CreateTable;
using RestaurantPOS.Application.Orders.Commands.RemoveOrderItem;
using RestaurantPOS.Application.Orders.Commands.ReopenOrder;
using RestaurantPOS.Application.Orders.Commands.ReprintReceipt;
using RestaurantPOS.Application.Orders.Commands.SetOrderDiscount;
using RestaurantPOS.Application.Orders.Commands.SetTableActive;
using RestaurantPOS.Application.Orders.Commands.StartCheckout;
using RestaurantPOS.Application.Orders.Commands.UpdateTable;
using RestaurantPOS.Application.Orders.Queries.GetOrderById;
using RestaurantPOS.Application.Orders.Queries.GetOrders;
using RestaurantPOS.Application.Orders.Queries.GetTables;
using RestaurantPOS.Domain.Enums;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Tables, orders, payments and receipts — everything the till does.</summary>
public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var tables = routes.MapGroup("/tables")
            .WithTags("Tables")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.PosBilling));

        MapTables(tables);

        var orders = routes.MapGroup("/orders")
            .WithTags("Orders")
            .RequireAuthorization(AuthorizationPolicies.ForModule(AppModule.PosBilling));

        MapOrders(orders);
        MapOrderItems(orders);
        MapCheckout(orders);

        return routes;
    }

    private static void MapTables(RouteGroupBuilder group)
    {
        group.MapGet("/", async (bool? isActive, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetTablesQuery(isActive), ct);
                return result.ToHttpResult();
            })
            .WithName("GetTables")
            .WithSummary("The floor plan, each table with whatever order is sitting on it.");

        group.MapPost("/", async (CreateTableRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateTableCommand(request.Number, request.Seats, request.Notes), ct);
                return result.ToCreatedResult(t => $"/api/v1/tables/{t.Id}");
            })
            .WithName("CreateTable")
            .WithSummary("Adds a table to the floor plan.");

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateTableRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateTableCommand(id, request.Number, request.Seats, request.Notes);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("UpdateTable")
            .WithSummary("Renames a table or changes its seat count.");

        group.MapPut("/{id:guid}/status", async (
                Guid id, SetTableActiveRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetTableActiveCommand(id, request.IsActive), ct);
                return result.ToHttpResult();
            })
            .WithName("SetTableActive")
            .WithSummary("Takes a table in or out of service.");
    }

    private static void MapOrders(RouteGroupBuilder group)
    {
        group.MapGet("/", async (
                bool? openOnly, OrderStatus? status, string? search, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOrdersQuery(openOnly ?? false, status, search), ct);
                return result.ToHttpResult();
            })
            .WithName("GetOrders")
            .WithSummary("Lists orders, optionally only those still holding a table.");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOrderByIdQuery(id), ct);
                return result.ToHttpResult();
            })
            .WithName("GetOrderById")
            .WithSummary("Loads one bill in full.");

        group.MapPost("/", async (CreateOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateOrderCommand(request.TableId), ct);
                return result.ToCreatedResult(o => $"/api/v1/orders/{o.Id}");
            })
            .WithName("CreateOrder")
            .WithSummary("Opens a draft bill on a table.");

        group.MapPost("/{id:guid}/confirm", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmOrderCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("ConfirmOrder")
            .WithSummary("Saves the order, numbers it and returns the KOT to print.");

        group.MapPost("/{id:guid}/cancel", async (
                Guid id, CancelOrderRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CancelOrderCommand(id, request.Pin, request.Reason), ct);
                return result.ToHttpResult();
            })
            .WithName("CancelOrder")
            .WithSummary("Abandons an order without payment. Needs an approval PIN once confirmed.");

        group.MapPut("/{id:guid}/discount", async (
                Guid id, SetOrderDiscountRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new SetOrderDiscountCommand(id, request.Type, request.Value), ct);
                return result.ToHttpResult();
            })
            .WithName("SetOrderDiscount")
            .WithSummary("Applies money off the bill.");
    }

    private static void MapOrderItems(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/items", async (
                Guid id, AddOrderItemsRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new AddOrderItemsCommand(
                    id,
                    [.. request.Items.Select(i => new AddOrderItemInput(i.MenuItemId, i.Quantity, i.SpecialInstructions))]);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("AddOrderItems")
            .WithSummary("Adds dishes to a bill. Never needs approval; prints a KOT once open.");

        group.MapPut("/{id:guid}/items/{itemId:guid}/quantity", async (
                Guid id, Guid itemId, ChangeOrderItemQuantityRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new ChangeOrderItemQuantityCommand(id, itemId, request.Quantity, request.Pin);
                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("ChangeOrderItemQuantity")
            .WithSummary("Changes how many of a dish were ordered. Needs an approval PIN once open.");

        // POST rather than DELETE: the PIN has to travel with the request, and a DELETE body is
        // both awkward to send and liable to be dropped in transit. On an open order this voids
        // the line rather than erasing it, so "void" is the truer verb in any case.
        group.MapPost("/{id:guid}/items/{itemId:guid}/void", async (
                Guid id, Guid itemId, RemoveOrderItemRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RemoveOrderItemCommand(id, itemId, request.Pin), ct);
                return result.ToHttpResult();
            })
            .WithName("VoidOrderItem")
            .WithSummary("Takes a dish off a bill. Needs an approval PIN once open.");
    }

    private static void MapCheckout(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/checkout", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new StartCheckoutCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("StartCheckout")
            .WithSummary("Freezes the bill and moves to the payment screen.");

        group.MapPost("/{id:guid}/reopen", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReopenOrderCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("ReopenOrder")
            .WithSummary("Backs out of the payment screen so more items can be added.");

        group.MapPost("/{id:guid}/payments", async (
                Guid id, CompleteOrderPaymentRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new CompleteOrderPaymentCommand(
                    id,
                    [.. request.Payments.Select(p => new OrderPaymentInput(
                        p.Method, p.Amount, p.TenderedAmount, p.Reference))]);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .WithName("CompleteOrderPayment")
            .WithSummary("Settles the bill, issues the receipt and releases the table.");

        group.MapPost("/{id:guid}/receipt/reprint", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new ReprintReceiptCommand(id), ct);
                return result.ToHttpResult();
            })
            .WithName("ReprintReceipt")
            .WithSummary("Prints a settled bill's receipt again.");
    }
}
