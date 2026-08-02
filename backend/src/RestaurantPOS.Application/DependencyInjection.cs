using FluentValidation;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using RestaurantPOS.Application.Common.Behaviors;

namespace RestaurantPOS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Order matters: logging wraps the whole pipeline, validation runs immediately
            // before the handler so handlers can assume valid input.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}