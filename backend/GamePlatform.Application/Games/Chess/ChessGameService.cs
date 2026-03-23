using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Rules;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Application.Games.Chess;

public class ChessGameService : IChessGameService
{
    private static readonly Dictionary<Guid, GameRoom> _games = [];

    public GameRoom CreateGame(string name)
    {
        var game = GameRoom.CreateChessRoom(name, new ChessMoveValidator());
        _games[game.Id] = game;
        return game;
    }

    public GameRoom GetGame(Guid id)
    {
        return _games.TryGetValue(id, out var game) ? game : throw new KeyNotFoundException("Game not found");
    }

    public void JoinRoom(Guid id, string userName, Side side)
    {
        var game = GetGame(id);
        game.JoinAsPlayer(userName, side);
    }

    public MoveResult MakeMove(Guid id, string playerName, Move move)
    {
        var game = GetGame(id);
        return game.MakeMove(playerName, move);
    }
}
