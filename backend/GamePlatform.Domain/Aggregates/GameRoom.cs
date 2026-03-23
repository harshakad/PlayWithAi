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
    public GameType Type { get; private set; }
    public GameStatus Status { get; private set; }
    public Side CurrentTurn { get; private set; }
    public Board Board { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Player> _players = [];
    public IReadOnlyList<Player> Players => _players.AsReadOnly();

    private readonly List<Move> _moveHistory = [];
    public IReadOnlyList<Move> MoveHistory => _moveHistory.AsReadOnly();

    private readonly IMoveValidator _moveValidator;

    // ── Factory Methods ────────────────────────────────────────

    public static GameRoom CreateChessRoom(string name, IMoveValidator moveValidator)
    {
        return new GameRoom(name, GameType.Chess, Board.CreateForChess(), Side.First, moveValidator);
    }

    public static GameRoom CreateCheckersRoom(string name, IMoveValidator moveValidator)
    {
        return new GameRoom(name, GameType.Checkers, Board.CreateForCheckers(), Side.First, moveValidator);
    }

    private GameRoom(string name, GameType type, Board board, Side startingTurn, IMoveValidator moveValidator)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new GameDomainException("Room name cannot be empty.");

        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        Board = board;
        CurrentTurn = startingTurn;
        Status = GameStatus.WaitingForPlayers;
        CreatedAt = DateTime.UtcNow;
        _moveValidator = moveValidator ?? throw new ArgumentNullException(nameof(moveValidator));
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
        if (Status != GameStatus.WaitingForPlayers && Status != GameStatus.InProgress)
            throw new GameDomainException("Cannot join a game that is completed or abandoned.");

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
            Status = GameStatus.InProgress;
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
        if (Status != GameStatus.InProgress)
            return MoveResult.Failure("Game is not in progress.");

        // Invariant: player must exist in this room
        var player = _players.FirstOrDefault(p => p.UserName == playerName);
        if (player is null)
            return MoveResult.Failure("You are not a player in this room.");

        // Invariant: must be this player's turn
        if (player.Side != CurrentTurn)
            return MoveResult.Failure("It is not your turn.");

        // Map player Side → PieceColor for the validator
        var movingColor = SideToPieceColor(player.Side, Type);

        // Delegate to game-specific validator
        var (isValid, reason) = _moveValidator.Validate(Board, move, movingColor);
        if (!isValid)
            return MoveResult.Failure(reason!);

        // Get the piece before moving (for side effects like promotion)
        var movedPiece = Board.GetPieceAt(move.From)!;

        // Apply the basic move
        var newBoard = Board.ApplyMove(move);

        // Apply game-specific side effects (captures, promotions)
        newBoard = _moveValidator.ApplySideEffects(newBoard, move, movedPiece);

        Board = newBoard;
        _moveHistory.Add(move);

        // Advance the turn
        var nextTurn = GetNextTurn();
        CurrentTurn = nextTurn;

        return MoveResult.Success(Board, nextTurn);
    }

    /// <summary>
    /// Marks the game as completed.
    /// </summary>
    public void CompleteGame()
    {
        if (Status != GameStatus.InProgress)
            throw new GameDomainException("Can only complete a game that is in progress.");

        Status = GameStatus.Completed;
    }

    /// <summary>
    /// Marks the game as abandoned.
    /// </summary>
    public void AbandonGame()
    {
        if (Status == GameStatus.Completed)
            throw new GameDomainException("Cannot abandon a completed game.");

        Status = GameStatus.Abandoned;
    }

    // ── Private Helpers ────────────────────────────────────────

    private Side GetNextTurn()
    {
        return CurrentTurn == Side.First ? Side.Second : Side.First;
    }

    private void ValidateSideForGameType(Side side)
    {
        // For now, both games use First and Second
        var validSides = new[] { Side.First, Side.Second };

        if (!validSides.Contains(side))
            throw new GameDomainException($"Side '{side}' is not valid.");
    }

    private static PieceColor SideToPieceColor(Side side, GameType gameType)
    {
        return gameType switch
        {
            GameType.Chess => side == Side.First ? PieceColor.White : PieceColor.Black,
            GameType.Checkers => side == Side.First ? PieceColor.Black : PieceColor.Red,
            _ => throw new GameDomainException("Unsupported game type.")
        };
    }
}