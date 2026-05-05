using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Application.Interfaces;

public interface IGameService
{
    IReadOnlyCollection<GameRoom> GameRooms { get; }

    GameRoom CreateRoom(GameType type, string name, bool isAgainstAi = false);

    GameRoom GetRoom(Guid id);

    GameRoom JoinRoom(Guid id, string userName, Side side);

    MoveResult MakeMove(Guid id, string playerName, Move move);

    MoveResult EndTurn(Guid id);
}