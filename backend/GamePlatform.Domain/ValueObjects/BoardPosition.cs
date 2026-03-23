namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Represents a position on the 8x8 board. Immutable value object.
/// </summary>
public sealed record BoardPosition
{
    public int Row { get; }
    public int Col { get; }

    public BoardPosition(int row, int col)
    {
        if (row < 0 || row > 7)
            throw new ArgumentOutOfRangeException(nameof(row), "Row must be between 0 and 7.");
        if (col < 0 || col > 7)
            throw new ArgumentOutOfRangeException(nameof(col), "Col must be between 0 and 7.");

        Row = row;
        Col = col;
    }

    public static implicit operator BoardPosition((int row, int col) pos) => new(pos.row, pos.col);

    public override string ToString() => $"({Row}, {Col})";
}