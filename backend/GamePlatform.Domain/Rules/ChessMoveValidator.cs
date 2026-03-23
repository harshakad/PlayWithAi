using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Domain.Rules;

/// <summary>
/// Basic chess move validator. Validates piece movement patterns.
/// Does NOT yet cover: castling, en passant, check/checkmate detection.
/// These can be added incrementally.
/// </summary>
public class ChessMoveValidator : IMoveValidator
{
    public (bool IsValid, string? Reason) Validate(Board board, Move move, PieceColor movingColor)
    {
        var piece = board.GetPieceAt(move.From);

        if (piece is null)
            return (false, "No piece at the source square.");

        if (piece.Color != movingColor)
            return (false, "That piece does not belong to you.");

        var target = board.GetPieceAt(move.To);
        if (target is not null && target.Color == movingColor)
            return (false, "Cannot capture your own piece.");

        var valid = piece.Type switch
        {
            PieceType.Pawn => ValidatePawnMove(board, move, piece),
            PieceType.Rook => ValidateRookMove(board, move),
            PieceType.Knight => ValidateKnightMove(move),
            PieceType.Bishop => ValidateBishopMove(board, move),
            PieceType.Queen => ValidateQueenMove(board, move),
            PieceType.King => ValidateKingMove(move),
            _ => (false, "Unknown piece type.")
        };

        return valid;
    }

    public Board ApplySideEffects(Board boardAfterMove, Move move, Piece movedPiece)
    {
        // Pawn promotion: if a pawn reaches the far rank, promote to Queen
        if (movedPiece.Type == PieceType.Pawn)
        {
            bool isPromotion = (movedPiece.Color == PieceColor.White && move.To.Row == 0)
                            || (movedPiece.Color == PieceColor.Black && move.To.Row == 7);

            if (isPromotion)
            {
                boardAfterMove = boardAfterMove.PromotePieceAt(move.To, PieceType.Queen);
            }
        }

        return boardAfterMove;
    }

    // ── Piece-Specific Validation ──────────────────────────────

    private static (bool, string?) ValidatePawnMove(Board board, Move move, Piece pawn)
    {
        int direction = pawn.Color == PieceColor.White ? -1 : 1; // White moves up (row decreasing)
        int rowDelta = move.RowDelta;
        int colDelta = Math.Abs(move.ColDelta);

        // Standard forward move
        if (colDelta == 0 && rowDelta == direction && board.IsEmpty(move.To))
            return (true, null);

        // Double move from starting row
        int startRow = pawn.Color == PieceColor.White ? 6 : 1;
        if (colDelta == 0 && rowDelta == 2 * direction && move.From.Row == startRow)
        {
            var intermediate = new BoardPosition(move.From.Row + direction, move.From.Col);
            if (board.IsEmpty(intermediate) && board.IsEmpty(move.To))
                return (true, null);
        }

        // Diagonal capture
        if (colDelta == 1 && rowDelta == direction && !board.IsEmpty(move.To))
            return (true, null);

        return (false, "Invalid pawn move.");
    }

    private static (bool, string?) ValidateRookMove(Board board, Move move)
    {
        if (move.From.Row != move.To.Row && move.From.Col != move.To.Col)
            return (false, "Rook must move in a straight line.");

        if (!IsPathClear(board, move))
            return (false, "Path is blocked.");

        return (true, null);
    }

    private static (bool, string?) ValidateKnightMove(Move move)
    {
        int absRow = Math.Abs(move.RowDelta);
        int absCol = Math.Abs(move.ColDelta);

        if ((absRow == 2 && absCol == 1) || (absRow == 1 && absCol == 2))
            return (true, null);

        return (false, "Invalid knight move.");
    }

    private static (bool, string?) ValidateBishopMove(Board board, Move move)
    {
        if (Math.Abs(move.RowDelta) != Math.Abs(move.ColDelta))
            return (false, "Bishop must move diagonally.");

        if (!IsPathClear(board, move))
            return (false, "Path is blocked.");

        return (true, null);
    }

    private static (bool, string?) ValidateQueenMove(Board board, Move move)
    {
        bool isStraight = move.From.Row == move.To.Row || move.From.Col == move.To.Col;
        bool isDiagonal = Math.Abs(move.RowDelta) == Math.Abs(move.ColDelta);

        if (!isStraight && !isDiagonal)
            return (false, "Queen must move in a straight line or diagonally.");

        if (!IsPathClear(board, move))
            return (false, "Path is blocked.");

        return (true, null);
    }

    private static (bool, string?) ValidateKingMove(Move move)
    {
        if (Math.Abs(move.RowDelta) <= 1 && Math.Abs(move.ColDelta) <= 1)
            return (true, null);

        return (false, "King can only move one square.");
    }

    // ── Path Checking ──────────────────────────────────────────

    private static bool IsPathClear(Board board, Move move)
    {
        int rowStep = Math.Sign(move.RowDelta);
        int colStep = Math.Sign(move.ColDelta);
        int currentRow = move.From.Row + rowStep;
        int currentCol = move.From.Col + colStep;

        while (currentRow != move.To.Row || currentCol != move.To.Col)
        {
            if (!board.IsEmpty(new BoardPosition(currentRow, currentCol)))
                return false;
            currentRow += rowStep;
            currentCol += colStep;
        }

        return true;
    }
}
