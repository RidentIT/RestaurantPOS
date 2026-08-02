using FluentValidation;

using MediatR;

using RestaurantPOS.Application.Common.Exceptions;

namespace RestaurantPOS.Application.Common.Behaviors;

/// <summary>
/// Runs every FluentValidation validator registered for a request before the handler sees it,
/// so handlers can assume well-formed input.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        return failures.Count > 0
            ? throw new ValidationAppException(failures)
            : await next(cancellationToken);
    }
}