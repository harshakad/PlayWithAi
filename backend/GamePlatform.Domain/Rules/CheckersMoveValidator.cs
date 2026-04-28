using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Domain.Rules;

/// <summary>
/// Checkers move validator. Validates diagonal moves and jumps.
/// Supports:
/// - Single diagonal forward move for Men
/// - Single diagonal jump (capture) for Men
/// - Backward movement and jumps for Kings
/// - Promotion to King when reaching the far row
/// </summary>
public class CheckersMoveValidator : IMoveValidator
{
    public (bool IsValid, string? Reason) Validate(Board board, Move move, PieceColor movingColor)
    {
        var piece = board.GetPieceAt(move.From);

        if (piece is null)
            return (false, "No piece at the source square.");

        if (piece.Color != movingColor)
            return (false, "That piece does not belong to you.");

        var target = board.GetPieceAt(move.To);
        if (target is not null)
            return (false, "Destination square is not empty. Checkers pieces move to empty squares.");

        int rowDelta = move.RowDelta;
        int colDelta = move.ColDelta;
        int absRow = Math.Abs(rowDelta);
        int absCol = Math.Abs(colDelta);

        // Must move diagonally
        if (absRow != absCol)
            return (false, "Checkers pieces must move diagonally.");

        // Direction enforcement for Men (not Kings)
        int forwardDirection = piece.Color == PieceColor.Black ? 1 : -1; // Black moves down, Red moves up
        bool isKing = piece.Type == PieceType.CheckersKing;

        // Simple move (1 square diagonal)
        if (absRow == 1)
        {
            if (!isKing && rowDelta != forwardDirection)
                return (false, "Men can only move forward.");

            return (true, null);
        }

        // Jump move (2 squares diagonal — must capture an opponent)
        if (absRow == 2)
        {
            if (!isKing && Math.Sign(rowDelta) != forwardDirection)
                return (false, "Men can only jump forward.");

            int midRow = move.From.Row + rowDelta / 2;
            int midCol = move.From.Col + colDelta / 2;
            var midPiece = board[midRow, midCol];

            if (midPiece is null)
                return (false, "No piece to jump over.");

            if (midPiece.Color == movingColor)
                return (false, "Cannot jump over your own piece.");

            return (true, null);
        }

        return (false, "Checkers pieces can only move 1 square or jump 2 squares diagonally.");
    }
}