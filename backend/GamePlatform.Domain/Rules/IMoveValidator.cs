using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Domain.Rules;

/// <summary>
/// Strategy interface for game-specific move validation.
/// Each game type (Chess, Checkers) provides its own implementation.
/// </summary>
public interface IMoveValidator
{
    /// <summary>
    /// Validates whether a move is legal given the current board state.
    /// Returns (isValid, errorReason).
    /// </summary>
    (bool IsValid, string? Reason) Validate(Board board, Move move, PieceColor movingColor);

    /// <summary>
    /// Applies any side effects for a valid move (e.g. captures, promotions)
    /// and returns the resulting board.
    /// The basic "move piece from A to B" is already applied before this is called.
    /// </summary>
    Board ApplySideEffects(Board boardAfterMove, Move move, Piece movedPiece);
}
