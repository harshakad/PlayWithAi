using System;
using System.Collections.Generic;
using GamePlatform.Domain.Entities;
using GamePlatform.Domain.Enums;

namespace GamePlatform.Application.Games.Checkers;

public class CheckersGameService : ICheckersGameService
{
    private static readonly Dictionary<Guid, GameRoom> _games = new();

    public GameRoom CreateGame(string name)
    {
        var game = new GameRoom
        {
            Name = name,
            Type = GameType.Checkers,
            State = "Initial Checkers State" 
        };
        _games[game.Id] = game;
        return game;
    }

    public GameRoom GetGame(Guid id)
    {
        return _games.TryGetValue(id, out var game) ? game : throw new KeyNotFoundException("Game not found");
    }

    public bool MakeMove(Guid id, string move)
    {
        if (_games.TryGetValue(id, out var game))
        {
            game.State = $"Moved {move}";
            return true;
        }
        return false;
    }
}
