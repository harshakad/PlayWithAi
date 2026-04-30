namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Encapsulates the result of attempting a move.
/// </summary>
public sealed class MoveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Board? NewBoard { get; }
    public bool IsGameOver { get; }
    public string? GameOverReason { get; } = null;

    private MoveResult(bool success, Board? newBoard, string? error, bool isGameOver = false, string? gameOverReason = null)
    {
        IsSuccess = success;
        NewBoard = newBoard;
        ErrorMessage = error;
        IsGameOver = isGameOver;
        GameOverReason = gameOverReason;
    }

    public static MoveResult Success(Board newBoard, bool isGameOver = false, string? gameOverReason = null) =>
        new(true, newBoard, null, isGameOver, gameOverReason);

    public static MoveResult Failure(string reason) =>
        new(false, null, reason);
}