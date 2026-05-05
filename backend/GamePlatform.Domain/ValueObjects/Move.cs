namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Represents a move from one square to another. Immutable value object.
/// </summary>
public sealed record Move
{
    public BoardPosition From { get; }
    public BoardPosition To { get; }
    public DateTime Timestamp { get; }

    /// <summary>
    /// Constructs a Move from a UCI(Universal Chess Interface) string (e.g. "e2e4"). (0,0) is a8.
    /// </summary>
    public Move(string uci)
    {
        if (uci is null || uci.Length < 4)
            throw new ArgumentException("UCI string must be at least 4 characters.", nameof(uci));

        From = ParseUciSquare(uci.Substring(0, 2));
        To = ParseUciSquare(uci.Substring(2, 2));

        if (From == To)
            throw new ArgumentException("Source and destination squares cannot be the same.");

        Timestamp = DateTime.UtcNow;
    }

    private static BoardPosition ParseUciSquare(string square)
    {
        if (square.Length != 2)
            throw new ArgumentException("Square must be 2 characters.", nameof(square));

        char file = square[0]; // 'a'..'h'
        char rank = square[1]; // '1'..'8'

        if (file < 'a' || file > 'h')
            throw new ArgumentException($"Invalid file: {file}");
        if (rank < '1' || rank > '8')
            throw new ArgumentException($"Invalid rank: {rank}");

        // (0,0) is a8, so:
        int col = file - 'a';
        int row = 8 - (rank - '0');
        return new BoardPosition(row, col);
    }
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