namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Represents a move from one square to another. Immutable value object.
/// </summary>
public sealed record Move
{
    public BoardPosition From { get; }
    public BoardPosition To { get; }
    public DateTime Timestamp { get; }

    public Move(BoardPosition from, BoardPosition to)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));

        if (from == to)
            throw new ArgumentException("Source and destination squares cannot be the same.");

        Timestamp = DateTime.UtcNow;
    }

    /// <summary>Row delta (positive = downward on the board).</summary>
    public int RowDelta => To.Row - From.Row;

    /// <summary>Column delta (positive = rightward on the board).</summary>
    public int ColDelta => To.Col - From.Col;

    public override string ToString() => $"{From} → {To}";
}
