using GamePlatform.Domain.Enums;

namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Represents the 8x8 board state. Treated as a value object — applying a move
/// returns a new Board instance with the mutation applied.
/// </summary>
public class Board(Piece?[,] pieces)
{
    public const int Size = 8;

    public Piece?[,] Pieces { get; } = pieces;

    public Piece? GetPieceAt(BoardPosition position) => Pieces[position.Row, position.Col];

    public Piece? this[int row, int col] => Pieces[row, col];

    /// <summary>
    /// Returns a new Board with the given move applied.
    /// Does NOT validate the move — that is the responsibility of the MoveValidator.
    /// </summary>
    public Board ApplyMove(Move move)
    {
        var newSquares = CloneSquares();
        newSquares[move.To.Row, move.To.Col] = newSquares[move.From.Row, move.From.Col];
        newSquares[move.From.Row, move.From.Col] = null;
        return new Board(newSquares);
    }

    /// <summary>
    /// Returns a new Board with a captured piece removed (for checkers jumps, etc.).
    /// </summary>
    public Board RemovePieceAt(BoardPosition square)
    {
        var newSquares = CloneSquares();
        newSquares[square.Row, square.Col] = null;
        return new Board(newSquares);
    }

    /// <summary>
    /// Returns a new Board with a piece promoted at the given square.
    /// </summary>
    public Board PromotePieceAt(BoardPosition square, PieceType newType)
    {
        var existing = Pieces[square.Row, square.Col]
            ?? throw new InvalidOperationException($"No piece at {square} to promote.");
        var newSquares = CloneSquares();
        newSquares[square.Row, square.Col] = new Piece(existing.Color, newType);
        return new Board(newSquares);
    }

    public bool IsEmpty(BoardPosition square) => Pieces[square.Row, square.Col] is null;

    public static bool IsInBounds(int row, int col) => row >= 0 && row < Size && col >= 0 && col < Size;

    private Piece?[,] CloneSquares()
    {
        var clone = new Piece?[Size, Size];
        Array.Copy(Pieces, clone, Pieces.Length);
        return clone;
    }
}

public class ChessBoard(Piece?[,] squares) : Board(squares)
{
    public static ChessBoard CreateNew()
    {
        var squares = new Piece?[Size, Size];

        // Black major pieces (row 0)
        squares[0, 0] = new Piece(PieceColor.Black, PieceType.Rook);
        squares[0, 1] = new Piece(PieceColor.Black, PieceType.Knight);
        squares[0, 2] = new Piece(PieceColor.Black, PieceType.Bishop);
        squares[0, 3] = new Piece(PieceColor.Black, PieceType.Queen);
        squares[0, 4] = new Piece(PieceColor.Black, PieceType.King);
        squares[0, 5] = new Piece(PieceColor.Black, PieceType.Bishop);
        squares[0, 6] = new Piece(PieceColor.Black, PieceType.Knight);
        squares[0, 7] = new Piece(PieceColor.Black, PieceType.Rook);

        // Black pawns (row 1)
        for (int col = 0; col < Size; col++)
            squares[1, col] = new Piece(PieceColor.Black, PieceType.Pawn);

        // White pawns (row 6)
        for (int col = 0; col < Size; col++)
            squares[6, col] = new Piece(PieceColor.White, PieceType.Pawn);

        // White major pieces (row 7)
        squares[7, 0] = new Piece(PieceColor.White, PieceType.Rook);
        squares[7, 1] = new Piece(PieceColor.White, PieceType.Knight);
        squares[7, 2] = new Piece(PieceColor.White, PieceType.Bishop);
        squares[7, 3] = new Piece(PieceColor.White, PieceType.Queen);
        squares[7, 4] = new Piece(PieceColor.White, PieceType.King);
        squares[7, 5] = new Piece(PieceColor.White, PieceType.Bishop);
        squares[7, 6] = new Piece(PieceColor.White, PieceType.Knight);
        squares[7, 7] = new Piece(PieceColor.White, PieceType.Rook);

        return new ChessBoard(squares);
    }
}

public class CheckersBoard(Piece?[,] squares) : Board(squares)
{
    public static CheckersBoard CreateNew()
    {
        var squares = new Piece?[Size, Size];

        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if ((row + col) % 2 == 1)
                {
                    if (row < 3)
                        squares[row, col] = new Piece(PieceColor.Black, PieceType.Man);
                    else if (row > 4)
                        squares[row, col] = new Piece(PieceColor.Red, PieceType.Man);
                }
            }
        }

        return new CheckersBoard(squares);
    }
}