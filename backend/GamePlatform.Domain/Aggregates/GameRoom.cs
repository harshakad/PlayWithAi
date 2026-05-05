using GamePlatform.Domain.Entities;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Exceptions;
using GamePlatform.Domain.Rules;
using GamePlatform.Domain.ValueObjects;

namespace GamePlatform.Domain.Aggregates;

/// <summary>
/// Aggregate Root for a game session. All state mutations go through this class,
/// which enforces all domain invariants.
/// </summary>
public class GameRoom
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }

    public Side CurrentTurn { get; private set; }
    public bool IsPlayingAgainstAi { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private readonly List<Player> _players = [];
    public IReadOnlyList<Player> Players => _players.AsReadOnly();

    public IGame Game { get; private set; }

    public GameRoom(string name, Side startingTurn, IGame game)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new GameDomainException("Room name cannot be empty.");

        Id = Guid.NewGuid();
        Name = name;

        CurrentTurn = startingTurn;
        CreatedAt = DateTime.UtcNow;
        Game = game;
    }

    // ── Behaviors ──────────────────────────────────────────────

    /// <summary>
    /// Adds a player to the room. Enforces:
    /// - Max 2 players
    /// - Side not already taken
    /// - No duplicate usernames
    /// Auto-starts the game when 2 players have joined.
    /// </summary>
    public void JoinAsPlayer(string userName, Side side)
    {
        if (!Game.IsJoinable)
            throw new GameDomainException("Cannot join a game that is started, completed or abandoned.");

        if (_players.Count >= 2)
            throw new GameDomainException("Room is full. Maximum 2 players.");

        if (_players.Any(p => p.Side == side))
            throw new GameDomainException($"Side '{side}' is already taken.");

        if (_players.Any(p => p.UserName == userName))
            throw new GameDomainException($"Player '{userName}' is already in this room.");

        ValidateSideForGameType(side);

        _players.Add(new Player(userName, side));

        if (_players.Count == 2)
        {
            Game.StartGame();
        }
    }

    /// <summary>
    /// Attempts to make a move. Enforces all invariants and delegates
    /// to the IMoveValidator for game-specific legality checks.
    /// Returns a MoveResult indicating success or failure with reason.
    /// </summary>
    public MoveResult MakeMove(string playerName, Move move)
    {
        // Invariant: game must be in progress
        if (Game.Status != GameStatus.InProgress)
            return MoveResult.Failure("Game is not in progress.");

        // Invariant: player must exist in this room
        var player = _players.FirstOrDefault(p => p.UserName == playerName);
        if (player is null)
            return MoveResult.Failure("You are not a player in this room.");

        // Invariant: must be this player's turn
        if (player.Side != CurrentTurn)
            return MoveResult.Failure("It is not your turn.");

        return Game.MakeMove(CurrentTurn, move);
    }

    public MoveResult EndTurn()
    {
        var result = Game.EndTurn();
        CurrentTurn = GetNextTurn();
        return result;

        Side GetNextTurn() => CurrentTurn == Side.First ? Side.Second : Side.First;
    }

    // ── Private Helpers ────────────────────────────────────────
    private void ValidateSideForGameType(Side side)
    {
        // For now, both games use First and Second
        var validSides = new[] { Side.First, Side.Second };

        if (!validSides.Contains(side))
            throw new GameDomainException($"Side '{side}' is not valid.");
    }

    public void PlayAgainstAi() => IsPlayingAgainstAi = true;
}