namespace RestaurantPOS.Domain.Enums;

/// <summary>
/// Where an expense sits in its approval life (EXP-015).
/// </summary>
/// <remarks>
/// <see cref="Pending"/> is not a compulsory stop. A manager recording the restaurant's own bills
/// approves them straight from <see cref="Draft"/>; Pending is what an expense enters when
/// somebody without approval rights submits one for a manager to decide on. Forcing a manager to
/// submit an expense to themselves before approving it would be ceremony, not control.
/// </remarks>
public enum ExpenseStatus
{
    /// <summary>Recorded but not yet put forward. Freely editable.</summary>
    Draft = 1,

    /// <summary>Submitted and waiting on a manager. Still editable until decided.</summary>
    Pending = 2,

    /// <summary>Signed off. Frozen, and the only state that counts towards reports (BR-EXP-005).</summary>
    Approved = 3,

    /// <summary>Turned down. Kept rather than deleted so the record survives (BR-EXP-006).</summary>
    Rejected = 4,
}
