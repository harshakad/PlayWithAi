using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.ComponentModel;

namespace GamePlatform.Infrastructure.Agents;

/// <summary>
/// Contains tool methods that the ChessAgent (ChatClientAgent) can invoke via function calling.
/// Each method is annotated with [Description] so the LLM understands when and how to use it.
/// </summary>
public sealed class ChessAgentTools(IGameService gameService, HubConnection hubConnection) : IDisposable
{
    private readonly StockfishTool _stockfish = new();
    private HubConnection? _connection = hubConnection;

    [Description("Get the best chess move for a given board state using the Stockfish engine. " +
                 "Input is a FEN string representing the current board state. " +
                 "Returns the best move in UCI format (e.g. 'e2e4').")]
    public async Task<string> GetStockfishMove(
        [Description("The current board state in FEN (Forsyth-Edwards Notation) format")] string fenBoardState)
    {
        Log.Information("Tool GetStockfishMove called with FEN: {Fen}", fenBoardState);
        var bestMove = await _stockfish.GetBestMoveAsync(fenBoardState);
        Log.Information("Stockfish suggests: {Move}", bestMove);
        return $"{{\"StockfishMove\": \"{bestMove}\"}}";
    }

    [Description("Play a chess move suggested by Stockfish")]
    public async Task<string> PlayStockfishMove(
        [Description("The room ID (GUID) of the game room")] string roomId,
        [Description("The player name making the move")] string playerName,
        [Description("The stockfish move in UCI format (e.g. 'e2e4')")] string stockfishMove)
    {
        Log.Information("Tool PlayStockfishMove called: room={RoomId}, player={Player}, move={Move}", roomId, playerName, stockfishMove);

        if (_connection is null)
            return "Error: SignalR connection not available.";

        try
        {
            var move = new Move(uci: stockfishMove);
            var moveResult = gameService.MakeMove(Guid.Parse(roomId), playerName, move);

            if (!moveResult.IsSuccess)
            {
                Log.Warning("Move {Move} failed: {Error}", stockfishMove, moveResult.ErrorMessage);
                return $"Move failed: {moveResult.ErrorMessage}. Please try a different move.";
            }

            // Broadcast the move to the room via SignalR
            await _connection.InvokeAsync("MakeMove", roomId, playerName, new
            {
                sourceRow = move.From.Row,
                sourceCol = move.From.Col,
                targetRow = move.To.Row,
                targetCol = move.To.Col,
                isGameOver = moveResult.IsGameOver,
                gameOverReason = moveResult.GameOverReason
            });

            if (!moveResult.IsGameOver)
            {
                // End the turn and broadcast the turn update
                gameService.EndTurn(Guid.Parse(roomId));
                var room = gameService.GetRoom(Guid.Parse(roomId));
                var nextPlayer = room.Players.FirstOrDefault(p => p.Side == room.CurrentTurn)?.UserName
                                 ?? room.CurrentTurn.ToString();
                var nextTurnSide = room.CurrentTurn == Side.First ? "first" : "second";

                await _connection.InvokeAsync("EndTurn", roomId, nextPlayer, nextTurnSide);
            }

            var result = moveResult.IsGameOver
                ? $"Move {stockfishMove} applied successfully. Game over: {moveResult.GameOverReason}"
                : $"Move {stockfishMove} applied successfully. Turn ended.";

            Log.Information("MakeGameMove result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error making move {Move} in room {RoomId}", stockfishMove, roomId);
            return $"Error making move: {ex.Message}";
        }
    }

    [Description("Send a chat message to players in a game room. " +
                 "Use this to greet opponents, comment on moves, or respond to chat messages.")]
    public async Task<string> SendChatMessage(
        [Description("The room ID (GUID) of the game room")] string roomId,
        [Description("The name of the player sending this message")] string playerName,
        [Description("The chat message to send")] string message)
    {
        Log.Information("Tool SendChatMessage called: room={RoomId}, message={Message}", roomId, message);

        if (_connection is null)
            return "Error: SignalR connection not available.";

        try
        {
            await _connection.InvokeAsync("SendMessage", roomId, playerName, message);
            return $"{{\"status\": \"Message sent successfully\"}}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending chat message to room {RoomId}", roomId);
            return $"{{\"error\": \"Error sending message: {ex.Message}\"}}";
        }
    }

    [Description("Get the current board state (FEN) for a given room. " +
                 "Use this to inspect the current board state before deciding on a move.")]
    public string GetBoardState(
        [Description("The room ID (GUID) of the game room")] string roomId)
    {
        Log.Information("Tool GetBoardState called: room={RoomId}", roomId);

        try
        {
            var room = gameService.GetRoom(Guid.Parse(roomId));
            var game = room.Game as ChessGame;
            var fen = game?.Board.ToFen(activeColor: room.CurrentTurn == Side.First ? PieceColor.White : PieceColor.Black) ?? "unknown";
            var currentTurn = room.CurrentTurn == Side.First ? "white" : "black";

            Log.Information("Current board state for room {RoomId}: FEN={Fen}, turn={Turn}", roomId, fen, currentTurn);
            return $"{{\"FEN\": \"{fen}\"}}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting board state for room {RoomId}", roomId);
            return $"Error: {ex.Message}";
        }
    }

    public void Dispose()
    {
        _stockfish.Dispose();
    }
}