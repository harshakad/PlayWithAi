using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.Entities.Checkers;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;

namespace GamePlatform.Application.Games
{
    public static class GameFactory
    {
        public static GameRoom CreateRoom(GameType type, string name)
        {
            return type switch
            {
                GameType.Chess => new GameRoom(name ?? "Chess Room", Side.First, new ChessGame()),
                GameType.Checkers => new GameRoom(name ?? "Checkers Room", Side.First, new CheckersGame()),
                _ => throw new ArgumentException("Invalid game type", nameof(type))
            };
        }
    }
}