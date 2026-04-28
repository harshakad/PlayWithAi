using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Exceptions;
using GamePlatform.Domain.Rules;
using GamePlatform.Domain.ValueObjects;

using System.Text.Json.Serialization;

namespace GamePlatform.Domain.Entities.Chess
{
    public class ChessGame : Game, IBoardGame
    {
        [JsonInclude]
        public Board Board { get; private set; } = ChessBoard.CreateNew();

        private Board? Pending { get; set; }

        public override GameType Type => GameType.Chess;

        private readonly ChessMoveValidator moveValidator = new();

        private static PieceColor GetSideColour(Side side) => side == Side.First ? PieceColor.White : PieceColor.Black;

        public override MoveResult MakeMove(Side currentTurn, Move move)
        {
            // Get the piece before moving (for side effects like promotion)
            var movedPiece = Board.GetPieceAt(move.From)!;

            // check if moved piece belongs to the current player
            if (movedPiece.Color != GetSideColour(currentTurn))
                return MoveResult.Failure("Cannot move opponent's piece.");

            // Delegate to game-specific validator
            var (isValid, reason) = moveValidator.Validate(Board, move, movedPiece.Color);
            if (!isValid)
                return MoveResult.Failure(reason!);

            // Apply the basic move
            var newBoard = Board.ApplyMove(move);

            // Apply game-specific side effects (captures, promotions)
            newBoard = newBoard.ApplyPromotions(move, movedPiece);

            // Check for checkmate, stalemate, etc. and update Status accordingly
            // ...

            Pending = newBoard;

            _moveHistory.Add(move);

            return MoveResult.Success(Pending);
        }

        public override MoveResult EndTurn()
        {
            if (Status != GameStatus.InProgress)
                throw new GameDomainException("Cannot end turn when game is not in progress.");

            Board = Pending ?? throw new GameDomainException("No pending board state to apply.");
            Pending = null;

            return MoveResult.Success(Board);
        }
    }
}