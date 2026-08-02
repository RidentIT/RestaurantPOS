using System.Text.Json;
using System.Threading.RateLimiting;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using RestaurantPOS.API.Endpoints;
using RestaurantPOS.API.Security;
using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Infrastructure.Identity;

namespace RestaurantPOS.API.Extensions;

/// <summary>Wires up bearer authentication, the module policies and abuse protection.</summary>
public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // The signing key and issuer settings come from the same options the token
                // service issues with, so the two can never disagree.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // A till and the server share a clock, so there is no reason to accept
                    // tokens past their stated expiry.
                    ClockSkew = TimeSpan.Zero,
                };
            });

        // Bound late so the signing key provider (a singleton) is resolved from the container
        // rather than constructed twice.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, JwtSigningKeyProvider>((bearer, jwtOptions, keyProvider) =>
            {
                bearer.TokenValidationParameters.ValidIssuer = jwtOptions.Value.Issuer;
                bearer.TokenValidationParameters.ValidAudience = jwtOptions.Value.Audience;
                bearer.TokenValidationParameters.IssuerSigningKey = keyProvider.SecurityKey;
            });

        services.AddAuthorizationBuilder()
            .AddAppPolicies()
            // Nothing is public unless it opts out with AllowAnonymous.
            .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build());

        return services;
    }

    /// <summary>
    /// Throttles endpoints that accept a guessable secret. The 4-digit approval PIN has only
    /// ten thousand combinations, so without this an attacker at an authenticated till could
    /// simply enumerate it.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthEndpoints.SensitiveRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    // Partition by user when signed in, otherwise by address, so one till
                    // hammering the PIN cannot lock out the others.
                    partitionKey: context.User.Identity?.Name
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Title = "Too many attempts. Please wait a moment and try again.",
                    Status = StatusCodes.Status429TooManyRequests,
                    Extensions = { ["code"] = "Auth.TooManyAttempts" },
                };

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(problem, ProblemJsonOptions),
                    cancellationToken);
            };
        });

        return services;
    }

    private static readonly JsonSerializerOptions ProblemJsonOptions =
        new(JsonSerializerDefaults.Web);
}