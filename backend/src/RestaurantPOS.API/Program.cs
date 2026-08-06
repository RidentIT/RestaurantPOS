using System.Text.Json.Serialization;

using Asp.Versioning;

using RestaurantPOS.API.Endpoints;
using RestaurantPOS.API.Extensions;
using RestaurantPOS.API.Middleware;
using RestaurantPOS.Application;
using RestaurantPOS.Infrastructure;
using RestaurantPOS.Infrastructure.Persistence.Seeding;

using Serilog;

#pragma warning disable CA1305
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
#pragma warning restore CA1305

try
{
    Log.Information("Starting RestaurantPOS API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // The Electron renderer loads from file:// (origin "null") in production and from
    // localhost in development, so origins are configurable rather than hard-coded.
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("PosClient", policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
            }
            else
            {
                policy.SetIsOriginAllowed(_ => true);
            }

            policy.AllowAnyHeader().AllowAnyMethod();
        });
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiAuthentication();
    builder.Services.AddApiRateLimiting();

    // Enums cross the wire as their names ("Admin", "PosBilling") rather than as integers, so
    // the client never has to mirror numeric values and payloads stay readable in logs.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Bring the database up to date and guarantee an administrator exists before serving.
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }

    app.UseMiddleware<GlobalExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseCors("PosClient");
    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // Runs after authentication so it can read the claim, and after authorization so an
    // anonymous caller gets a 401 rather than this middleware's 403.
    app.UseMiddleware<PasswordChangeRequiredMiddleware>();

    var versionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .ReportApiVersions()
        .Build();

    var api = app.MapGroup("/api/v{version:apiVersion}").WithApiVersionSet(versionSet);

    api.MapAuthEndpoints();
    api.MapUserEndpoints();
    api.MapModuleEndpoints();
    api.MapRecipeEndpoints();
    api.MapInventoryEndpoints();
    api.MapSupplierEndpoints();
    api.MapOrderEndpoints();
    api.MapKitchenEndpoints();
    api.MapExpenseEndpoints();

    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
        .AllowAnonymous()
        .WithTags("Diagnostics");

    await app.RunAsync();
}
// HostAbortedException is how the EF Core design-time tools stop the host after building the
// service provider; it is normal control flow, not a crash.
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "RestaurantPOS API terminated unexpectedly");

    // Rethrow so the process exits non-zero and a service manager notices the failure.
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program { }