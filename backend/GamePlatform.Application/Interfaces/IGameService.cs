using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Application.Interfaces;

public interface IGameService
{
    GameRoom CreateGame(string name);
    GameRoom GetGame(Guid id);
    void JoinRoom(Guid id, string userName, Side side);
    MoveResult MakeMove(Guid id, string playerName, Move move);
}
