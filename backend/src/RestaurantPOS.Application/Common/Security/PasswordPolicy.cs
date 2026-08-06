using FluentValidation;

namespace RestaurantPOS.Application.Common.Security;

/// <summary>
/// The single definition of what makes an acceptable password. Applied by every command that
/// accepts one so the rules cannot drift between sign-up, reset and change flows.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>Human-readable summary, shown by the frontend next to password fields.</summary>
    public const string Description =
        "Password must be at least 8 characters and include an upper-case letter, a lower-case letter and a digit.";

    /// <summary>Applies the policy to a string property on a FluentValidation rule chain.</summary>
    public static IRuleBuilderOptions<T, string> MustMeetPasswordPolicy<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);

        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(MinLength).WithMessage($"Password must be at least {MinLength} characters.")
            .MaximumLength(MaxLength).WithMessage($"Password cannot exceed {MaxLength} characters.")
            .Must(ContainsUpper).WithMessage("Password must contain an upper-case letter.")
            .Must(ContainsLower).WithMessage("Password must contain a lower-case letter.")
            .Must(ContainsDigit).WithMessage("Password must contain a digit.");
    }

    private static bool ContainsUpper(string value) => value.Any(char.IsUpper);

    private static bool ContainsLower(string value) => value.Any(char.IsLower);

    private static bool ContainsDigit(string value) => value.Any(char.IsDigit);
}