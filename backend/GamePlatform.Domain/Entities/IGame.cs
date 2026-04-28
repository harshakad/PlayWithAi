using GamePlatform.Domain.Entities.Checkers;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace GamePlatform.Domain.Entities
{
    [JsonDerivedType(typeof(ChessGame), typeDiscriminator: "chess")]
    [JsonDerivedType(typeof(CheckersGame), typeDiscriminator: "checkers")]
    public interface IGame
    {
        IReadOnlyList<Move> MoveHistory { get; }
        GameStatus Status { get; }
        GameType Type { get; }
        bool IsJoinable { get; }

        MoveResult EndTurn();

        MoveResult MakeMove(Side currentTurn, Move move);

        void StartGame();
    }

    public interface IBoardGame : IGame
    {
        Board Board { get; }
    }
}