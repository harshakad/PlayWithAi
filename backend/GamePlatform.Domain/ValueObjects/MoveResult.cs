namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Encapsulates the result of attempting a move.
/// </summary>
public sealed class MoveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Board? NewBoard { get; }

    private MoveResult(bool success, Board? newBoard, string? error)
    {
        IsSuccess = success;
        NewBoard = newBoard;
        ErrorMessage = error;
    }

    public static MoveResult Success(Board newBoard) =>
        new(true, newBoard, null);

    public static MoveResult Failure(string reason) =>
        new(false, null, reason);
}