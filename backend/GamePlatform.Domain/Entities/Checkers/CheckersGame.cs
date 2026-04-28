using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Exceptions;
using GamePlatform.Domain.Rules;
using GamePlatform.Domain.ValueObjects;

using System.Text.Json.Serialization;

namespace GamePlatform.Domain.Entities.Checkers
{
    public class CheckersGame : Game, IBoardGame
    {
        [JsonInclude]
        public Board Board { get; private set; } = CheckersBoard.CreateNew();

        private Board? Pending { get; set; }

        public override GameType Type => GameType.Checkers;

        private readonly CheckersMoveValidator moveValidator = new();

        private static PieceColor GetSideColour(Side side) => side == Side.First ? PieceColor.Black : PieceColor.Red;

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
            newBoard = newBoard.CapturePiece(move, movedPiece)
                               .ApplyPromotions(move, movedPiece);

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