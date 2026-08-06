using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Domain.Entities;

/// <summary>
/// A table customers sit at, identified by the number printed on it. Occupancy is deliberately
/// <em>not</em> stored here: a table is occupied exactly when it has a live order, so deriving it
/// at read time means the two can never disagree — no repair job for a table left "occupied"
/// after its order was completed by some path that forgot to clear the flag (POS-031, POS-032).
/// </summary>
public sealed class RestaurantTable : BaseEntity
{
    public const int NumberMaxLength = 20;
    public const int NotesMaxLength = 200;

    // EF Core materialisation.
    private RestaurantTable()
    {
    }

    private RestaurantTable(string number, int seats, string? notes)
    {
        Number = NormaliseNumber(number);
        Seats = ValidateSeats(seats);
        Notes = NormaliseNotes(notes);
        IsActive = true;
    }

    /// <summary>The table's label as the staff say it out loud — "4", "A2", "Terrace 1".</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>How many covers it seats. Zero means unrecorded.</summary>
    public int Seats { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; }

    public static RestaurantTable Create(string number, int seats = 0, string? notes = null) =>
        new(number, seats, notes);

    public void UpdateDetails(string number, int seats, string? notes)
    {
        Number = NormaliseNumber(number);
        Seats = ValidateSeats(seats);
        Notes = NormaliseNotes(notes);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string NormaliseNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        var trimmed = number.Trim();

        return trimmed.Length > NumberMaxLength
            ? throw new ArgumentException($"Table number cannot exceed {NumberMaxLength} characters.", nameof(number))
            : trimmed;
    }

    private static int ValidateSeats(int seats) =>
        seats >= 0
            ? seats
            : throw new ArgumentOutOfRangeException(nameof(seats), seats, "Seats cannot be negative.");

    private static string? NormaliseNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();

        return trimmed.Length > NotesMaxLength
            ? throw new ArgumentException($"Notes cannot exceed {NotesMaxLength} characters.", nameof(notes))
            : trimmed;
    }
}
