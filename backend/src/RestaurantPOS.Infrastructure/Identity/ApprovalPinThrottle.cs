using System.Collections.Concurrent;

using RestaurantPOS.Application.Common.Interfaces;

namespace RestaurantPOS.Infrastructure.Identity;

/// <summary>
/// In-memory approval-PIN rate limiter.
/// </summary>
/// <remarks>
/// Memory is the right store here rather than the database: the restaurant runs a single POS
/// process against a local SQLite file, so there is nothing to share state with, and a lockout
/// that evaporates if the machine is restarted is the correct behaviour anyway — a manager
/// restarting the till to get back in is a perfectly good escape hatch from a mistyped PIN.
/// </remarks>
internal sealed class ApprovalPinThrottle(IDateTimeProvider clock) : IApprovalPinThrottle
{
    /// <summary>Tries allowed before entry pauses, matching the 3 attempts the POS spec calls for.</summary>
    private const int MaxAttempts = 3;

    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, Attempts> _attempts = new(StringComparer.Ordinal);

    public ApprovalPinAttemptState Check(string terminalKey)
    {
        if (!_attempts.TryGetValue(terminalKey, out var record))
        {
            return new ApprovalPinAttemptState(IsLocked: false, TimeSpan.Zero, MaxAttempts);
        }

        var remainingLock = record.LockedUntilUtc - clock.UtcNow;

        if (remainingLock > TimeSpan.Zero)
        {
            return new ApprovalPinAttemptState(IsLocked: true, remainingLock, AttemptsRemaining: 0);
        }

        // The lockout has elapsed, so the slate is wiped rather than leaving the terminal one
        // failure away from an immediate re-lock.
        if (record.LockedUntilUtc != default)
        {
            _attempts.TryRemove(terminalKey, out _);
            return new ApprovalPinAttemptState(IsLocked: false, TimeSpan.Zero, MaxAttempts);
        }

        return new ApprovalPinAttemptState(IsLocked: false, TimeSpan.Zero, MaxAttempts - record.Failures);
    }

    public ApprovalPinAttemptState RecordFailure(string terminalKey)
    {
        var updated = _attempts.AddOrUpdate(
            terminalKey,
            _ => new Attempts(1, default),
            (_, existing) => existing with { Failures = existing.Failures + 1 });

        if (updated.Failures < MaxAttempts)
        {
            return new ApprovalPinAttemptState(IsLocked: false, TimeSpan.Zero, MaxAttempts - updated.Failures);
        }

        var lockedUntil = clock.UtcNow + LockoutDuration;
        _attempts[terminalKey] = new Attempts(updated.Failures, lockedUntil);

        return new ApprovalPinAttemptState(IsLocked: true, LockoutDuration, AttemptsRemaining: 0);
    }

    public void Reset(string terminalKey) => _attempts.TryRemove(terminalKey, out _);

    private sealed record Attempts(int Failures, DateTime LockedUntilUtc);
}
