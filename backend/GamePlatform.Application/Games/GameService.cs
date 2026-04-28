using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.Entities;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using System.Collections.Concurrent;

namespace GamePlatform.Application.Games;

public class GameService : IGameService
{
    private static readonly ConcurrentDictionary<Guid, GameRoom> rooms = [];

    public GameRoom CreateRoom(GameType type, string name)
    {
        var room = GameFactory.CreateRoom(type, name);
        room = rooms.GetOrAdd(room.Id, room);
        return room;
    }

    public GameRoom GetRoom(Guid id)
    {
        return rooms.TryGetValue(id, out var game) ? game : throw new KeyNotFoundException("Game not found");
    }

    public GameRoom JoinRoom(Guid id, string userName, Side side)
    {
        var room = GetRoom(id);
        room.JoinAsPlayer(userName, side);
        return room;
    }

    public MoveResult MakeMove(Guid id, string playerName, Move move)
    {
        var room = GetRoom(id);
        return room.MakeMove(playerName, move);
    }

    public MoveResult EndTurn(Guid id)
    {
        var room = GetRoom(id);
        return room.EndTurn();
    }
}