using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Domain.Rules;

/// <summary>
/// Basic chess move validator. Validates piece movement patterns.
/// Does NOT yet cover: castling, en passant, checkmate detection.
/// Includes check detection: validates that after a move, the moving player's king is not in check.
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
        if (target?.Color == movingColor)
            return (false, "Cannot capture your own piece.");

        if (target?.Type == PieceType.King)
            return (false, "Cannot capture the king.");

        var (isValid, reason) = piece.Type switch
        {
            PieceType.Pawn => ValidatePawnMove(board, move, piece),
            PieceType.Rook => ValidateRookMove(board, move),
            PieceType.Knight => ValidateKnightMove(move),
            PieceType.Bishop => ValidateBishopMove(board, move),
            PieceType.Queen => ValidateQueenMove(board, move),
            PieceType.King => ValidateKingMove(move),
            _ => (false, "Unknown piece type.")
        };

        if (!isValid)
            return (false, reason);

        // After applying the move, verify that the moving player's king is not in check
        var boardAfterMove = board.ApplyMove(move);
        if (IsKingInCheck(boardAfterMove, movingColor))
            return (false, "Your king would be in check.");

        return (true, null);
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

    // ── Check Detection ────────────────────────────────────────

    /// <summary>
    /// Determines if the king of the given color is in check on the board.
    /// A king is in check if any opponent piece can attack its square.
    /// </summary>
    private static bool IsKingInCheck(Board board, PieceColor kingColor)
    {
        var kingPosition = FindKing(board, kingColor);
        if (kingPosition is null)
            return false; // No king found (shouldn't happen in valid game)

        var opponentColor = kingColor == PieceColor.White ? PieceColor.Black : PieceColor.White;

        // Check if any opponent piece can attack the king's square
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var piece = board[row, col];
                if (piece is null || piece.Color != opponentColor)
                    continue;

                var attackMove = new Move(new BoardPosition(row, col), kingPosition);
                if (CanPieceAttack(board, attackMove, piece))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the position of the king of the given color on the board.
    /// </summary>
    private static BoardPosition? FindKing(Board board, PieceColor color)
    {
        for (int row = 0; row < Board.Size; row++)
        {
            for (int col = 0; col < Board.Size; col++)
            {
                var piece = board[row, col];
                if (piece?.Type == PieceType.King && piece.Color == color)
                    return new BoardPosition(row, col);
            }
        }

        return null;
    }

    /// <summary>
    /// Determines if a piece can attack (move to and potentially capture) a target square.
    /// Used for check detection. Does not validate turn order or leave the king in check.
    /// </summary>
    private static bool CanPieceAttack(Board board, Move move, Piece attackingPiece)
    {
        var target = board.GetPieceAt(move.To);

        // Can only attack if square is empty or contains an opponent piece
        if (target?.Color == attackingPiece.Color)
            return false;

        // Cannot "attack" the opponent's king (kings don't capture each other in check detection)
        //if (target?.Type == PieceType.King)
        //    return false;

        var canAttack = attackingPiece.Type switch
        {
            PieceType.Pawn => CanPawnAttack(board, move, attackingPiece),
            PieceType.Rook => CanRookAttack(board, move),
            PieceType.Knight => CanKnightAttack(move),
            PieceType.Bishop => CanBishopAttack(board, move),
            PieceType.Queen => CanQueenAttack(board, move),
            PieceType.King => CanKingAttack(move),
            _ => false
        };

        return canAttack;
    }

    private static bool CanPawnAttack(Board board, Move move, Piece pawn)
    {
        int direction = pawn.Color == PieceColor.White ? -1 : 1;
        int rowDelta = move.RowDelta;
        int colDelta = Math.Abs(move.ColDelta);

        // Pawns attack diagonally one square forward
        if (colDelta == 1 && rowDelta == direction)
            return true;

        return false;
    }

    private static bool CanRookAttack(Board board, Move move)
    {
        if (move.From.Row != move.To.Row && move.From.Col != move.To.Col)
            return false;

        return IsPathClear(board, move);
    }

    private static bool CanKnightAttack(Move move)
    {
        int absRow = Math.Abs(move.RowDelta);
        int absCol = Math.Abs(move.ColDelta);

        return (absRow == 2 && absCol == 1) || (absRow == 1 && absCol == 2);
    }

    private static bool CanBishopAttack(Board board, Move move)
    {
        if (Math.Abs(move.RowDelta) != Math.Abs(move.ColDelta))
            return false;

        return IsPathClear(board, move);
    }

    private static bool CanQueenAttack(Board board, Move move)
    {
        bool isStraight = move.From.Row == move.To.Row || move.From.Col == move.To.Col;
        bool isDiagonal = Math.Abs(move.RowDelta) == Math.Abs(move.ColDelta);

        if (!isStraight && !isDiagonal)
            return false;

        return IsPathClear(board, move);
    }

    private static bool CanKingAttack(Move move)
    {
        return Math.Abs(move.RowDelta) <= 1 && Math.Abs(move.ColDelta) <= 1;
    }

    // ── Public Game State Detection ────────────────────────────

    /// <summary>
    /// Determines if the king of the given color is in check on the board.
    /// Public version for external use (e.g., from ChessGame).
    /// </summary>
    public bool IsKingInCheckPublic(Board board, PieceColor kingColor)
    {
        return IsKingInCheck(board, kingColor);
    }

    /// <summary>
    /// Determines if the player of the given color is in checkmate.
    /// Checkmate = king is in check AND player has no legal moves.
    /// </summary>
    public bool IsCheckmate(Board board, PieceColor playerColor)
    {
        // Must be in check first
        if (!IsKingInCheck(board, playerColor))
            return false;

        // Check if player has any legal moves
        return !HasLegalMove(board, playerColor);
    }

    /// <summary>
    /// Determines if the player of the given color is in stalemate.
    /// Stalemate = king is NOT in check AND player has no legal moves.
    /// </summary>
    public bool IsStalemate(Board board, PieceColor playerColor)
    {
        // Must NOT be in check
        if (IsKingInCheck(board, playerColor))
            return false;

        // Check if player has any legal moves
        return !HasLegalMove(board, playerColor);
    }

    /// <summary>
    /// Determines if the player of the given color has at least one legal move.
    /// A legal move is one that doesn't leave their king in check.
    /// </summary>
    private bool HasLegalMove(Board board, PieceColor playerColor)
    {
        // Iterate through all squares
        for (int fromRow = 0; fromRow < Board.Size; fromRow++)
        {
            for (int fromCol = 0; fromCol < Board.Size; fromCol++)
            {
                var piece = board[fromRow, fromCol];
                if (piece is null || piece.Color != playerColor)
                    continue;

                // Try all possible destination squares
                for (int toRow = 0; toRow < Board.Size; toRow++)
                {
                    for (int toCol = 0; toCol < Board.Size; toCol++)
                    {
                        if (fromRow == toRow && fromCol == toCol)
                            continue;

                        var move = new Move(new BoardPosition(fromRow, fromCol), new BoardPosition(toRow, toCol));

                        // Check if this move is legal
                        var (isValid, _) = Validate(board, move, playerColor);
                        if (isValid)
                            return true; // Found at least one legal move
                    }
                }
            }
        }

        return false; // No legal moves found
    }
}