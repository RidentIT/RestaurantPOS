using System.Collections.ObjectModel;

using FluentValidation.Results;

namespace RestaurantPOS.Application.Common.Exceptions;

/// <summary>
/// Raised when request validation fails. The API surfaces this as an RFC 7807 validation
/// problem with one entry per offending field.
/// </summary>
public sealed class ValidationAppException : Exception
{
    public ValidationAppException()
        : this("One or more validation errors occurred.")
    {
    }

    public ValidationAppException(string message)
        : base(message)
    {
        Errors = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal));
    }

    public ValidationAppException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new ReadOnlyDictionary<string, string[]>(new Dictionary<string, string[]>(StringComparer.Ordinal));
    }

    public ValidationAppException(IEnumerable<ValidationFailure> failures)
        : this("One or more validation errors occurred.")
    {
        ArgumentNullException.ThrowIfNull(failures);

        var grouped = failures
            .GroupBy(f => f.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        Errors = new ReadOnlyDictionary<string, string[]>(grouped);
    }

    /// <summary>Validation messages keyed by the property that failed.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}