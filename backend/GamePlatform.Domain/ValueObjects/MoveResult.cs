namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Encapsulates the result of attempting a move.
/// </summary>
public sealed class MoveResult
{
    public bool IsSuccess { get; }
    public string? ErrorMessage { get; }
    public Board? NewBoard { get; }
    public Enums.Side? NextTurn { get; }

    private MoveResult(bool success, Board? newBoard, Enums.Side? nextTurn, string? error)
    {
        IsSuccess = success;
        NewBoard = newBoard;
        NextTurn = nextTurn;
        ErrorMessage = error;
    }

    public static MoveResult Success(Board newBoard, Enums.Side nextTurn) =>
        new(true, newBoard, nextTurn, null);

    public static MoveResult Failure(string reason) =>
        new(false, null, null, reason);
}
