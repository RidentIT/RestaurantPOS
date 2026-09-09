using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

using RestaurantPOS.API.Contracts.Settings;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Settings.Commands.CreateBackup;
using RestaurantPOS.Application.Settings.Commands.RemoveLogo;
using RestaurantPOS.Application.Settings.Commands.RestoreBackup;
using RestaurantPOS.Application.Settings.Commands.RunDailyBackup;
using RestaurantPOS.Application.Settings.Commands.UpdateApprovalPinPolicy;
using RestaurantPOS.Application.Settings.Commands.UpdateBackupSettings;
using RestaurantPOS.Application.Settings.Commands.UpdateBillCharges;
using RestaurantPOS.Application.Settings.Commands.UpdateBusinessProfile;
using RestaurantPOS.Application.Settings.Commands.UpdateDefaultPrinter;
using RestaurantPOS.Application.Settings.Commands.UpdateReceiptFooter;
using RestaurantPOS.Application.Settings.Commands.UploadLogo;
using RestaurantPOS.Application.Settings.Queries.GetBackups;
using RestaurantPOS.Application.Settings.Queries.GetBranding;
using RestaurantPOS.Application.Settings.Queries.GetLogo;
using RestaurantPOS.Application.Settings.Queries.GetRestaurantSettings;

namespace RestaurantPOS.API.Endpoints;

/// <summary>Business profile, bill charges, receipt, security and backup configuration.</summary>
public static class SettingsEndpoints
{
    private const long MaxLogoBytes = 2 * 1024 * 1024;

    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        // Deliberately not AdminOnly at the group level: RunDailyBackup below is called by whoever
        // signs in first each day, cashier or administrator alike, so every other endpoint applies
        // the AdminOnly policy for itself instead of inheriting one that would wrongly cover it too.
        // The branding and logo GETs go further still and allow anonymous access — the sign-in
        // screen, which by definition has no session yet, is one of the places they're shown.
        var group = routes.MapGroup("/settings")
            .WithTags("Settings")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRestaurantSettingsQuery(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("GetRestaurantSettings")
            .WithSummary("Every configurable value about the restaurant, as it stands today.");

        group.MapPut("/profile", async (UpdateBusinessProfileRequest request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateBusinessProfileCommand(
                    request.Name, request.AddressLine1, request.AddressLine2, request.City, request.Phone,
                    request.LogoPath, request.VatRegistrationNumber);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateBusinessProfile")
            .WithSummary("Sets the name, address and phone printed on every receipt and KOT.");

        group.MapGet("/branding", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetBrandingQuery(), ct);
                return result.ToHttpResult();
            })
            .AllowAnonymous()
            .WithName("GetBranding")
            .WithSummary("The restaurant's name, for the sign-in screen.");

        group.MapGet("/logo", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetLogoQuery(), ct);

                if (result.IsFailure)
                {
                    return result.ToHttpResult();
                }

                var logo = result.Value;

                return Results.File(logo.Content, logo.ContentType, logo.FileName);
            })
            .AllowAnonymous()
            .WithName("GetLogo")
            .WithSummary("The restaurant's logo image, or a 404 if none has been uploaded.");

        group.MapPost("/logo", async (IFormFile file, ISender sender, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();

                var command = new UploadLogoCommand(
                    stream, file.FileName, file.ContentType ?? "application/octet-stream", file.Length);

                var result = await sender.Send(command, ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UploadLogo")
            .WithSummary("Replaces the restaurant's logo.")
            .DisableAntiforgery()
            .WithMetadata(new RequestSizeLimitAttribute(MaxLogoBytes));

        group.MapDelete("/logo", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RemoveLogoCommand(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("RemoveLogo")
            .WithSummary("Clears the restaurant's logo.");

        group.MapPut("/bill-charges", async (UpdateBillChargesRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateBillChargesCommand(request.TaxRatePercent, request.ServiceChargeRatePercent), ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateBillCharges")
            .WithSummary("Sets the VAT and service charge percentages the next new order will carry.");

        group.MapPut("/receipt-footer", async (UpdateReceiptFooterRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new UpdateReceiptFooterCommand(request.Message), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateReceiptFooter")
            .WithSummary("Sets the line printed at the bottom of every receipt.");

        group.MapPut("/printer", async (UpdateDefaultPrinterRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateDefaultPrinterCommand(request.PrinterName, request.KitchenPrinterName), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateDefaultPrinter")
            .WithSummary("Sets which printer receipts are sent to, and which kitchen tickets are sent to.");

        group.MapPut("/approval-pin-policy", async (
                UpdateApprovalPinPolicyRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateApprovalPinPolicyCommand(request.MaxAttempts, request.LockoutMinutes), ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateApprovalPinPolicy")
            .WithSummary("Sets how many wrong PIN tries a terminal allows before it pauses, and for how long.");

        group.MapPut("/backup", async (UpdateBackupSettingsRequest request, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateBackupSettingsCommand(request.BackupFolderPath, request.RetentionCount), ct);

                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("UpdateBackupSettings")
            .WithSummary("Sets where backups are written and how many are kept.");

        group.MapGet("/backups", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetBackupsQuery(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("GetBackups")
            .WithSummary("Every backup archive on disk, newest first.");

        group.MapPost("/backups", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new CreateBackupCommand(), ct);
                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("CreateBackup")
            .WithSummary("Takes a backup right now.");

        group.MapPost("/backups/restore", async (
                RestoreBackupRequest request, ISender sender, IHostApplicationLifetime lifetime, CancellationToken ct) =>
            {
                var command = new RestoreBackupCommand(request.FileName, request.Pin, request.ConfirmationText);
                var result = await sender.Send(command, ct);

                if (result.IsSuccess)
                {
                    // The database this process has open no longer matches what is on disk, so the
                    // whole application has to come down — the response is left to flush to the
                    // client first, which is what a short delay before stopping the host buys.
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        lifetime.StopApplication();
                    });
                }

                return result.ToHttpResult();
            })
            .RequireAuthorization(AuthorizationPolicies.AdminOnly)
            .WithName("RestoreBackup")
            .WithSummary("Replaces every record with a backup archive, then shuts the API down.");

        group.MapPost("/backups/run-daily", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new RunDailyBackupCommand(), ct);
                return result.ToHttpResult();
            })
            .WithName("RunDailyBackup")
            .WithSummary("Takes today's backup if one has not already been taken today. Called once at sign-in.");

        return routes;
    }
}
