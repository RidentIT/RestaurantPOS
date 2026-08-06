namespace RestaurantPOS.Application.Common.Interfaces;

/// <summary>The state of one terminal's approval-PIN attempts.</summary>
/// <param name="IsLocked">True when PIN entry is paused and no attempt should be checked.</param>
/// <param name="RetryAfter">How long until entry reopens. Zero unless locked.</param>
/// <param name="AttemptsRemaining">Tries left before a lockout. Zero once locked.</param>
public readonly record struct ApprovalPinAttemptState(bool IsLocked, TimeSpan RetryAfter, int AttemptsRemaining);

/// <summary>
/// Rate-limits approval-PIN guessing per terminal.
/// </summary>
/// <remarks>
/// Scoped to the signed-in cashier rather than to an administrator, because the PIN is checked
/// against every administrator at once — there is no single account a failed guess belongs to.
/// Pausing the terminal also keeps a mistyped PIN from disabling the manager who was not even
/// standing there, which on a single-admin install would lock the restaurant out of its own till.
/// </remarks>
public interface IApprovalPinThrottle
{
    /// <summary>Reports whether <paramref name="terminalKey"/> may attempt a PIN right now.</summary>
    ApprovalPinAttemptState Check(string terminalKey);

    /// <summary>Counts a wrong PIN, locking the terminal once the allowance runs out.</summary>
    ApprovalPinAttemptState RecordFailure(string terminalKey);

    /// <summary>Clears the count after a correct PIN.</summary>
    void Reset(string terminalKey);
}
