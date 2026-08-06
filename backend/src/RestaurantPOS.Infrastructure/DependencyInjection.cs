using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Infrastructure.Clock;
using RestaurantPOS.Infrastructure.Identity;
using RestaurantPOS.Infrastructure.Persistence;
using RestaurantPOS.Infrastructure.Persistence.Interceptors;
using RestaurantPOS.Infrastructure.Persistence.Seeding;
using RestaurantPOS.Infrastructure.Settings;

namespace RestaurantPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedAdminOptions>()
            .Bind(configuration.GetSection(SeedAdminOptions.SectionName));

        var restaurantProfile = new RestaurantProfileOptions();
        configuration.GetSection(RestaurantProfileOptions.SectionName).Bind(restaurantProfile);
        services.AddSingleton<IRestaurantProfile>(restaurantProfile);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IApprovalPinThrottle, ApprovalPinThrottle>();
        services.AddSingleton<JwtSigningKeyProvider>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options
                .UseSqlite(
                    configuration.GetConnectionString("DefaultConnection"),
                    // The collections hanging off a user are tiny, so one round trip beats the
                    // extra queries splitting would cost.
                    sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}