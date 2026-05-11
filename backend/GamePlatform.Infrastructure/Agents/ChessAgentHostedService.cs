using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace GamePlatform.Infrastructure.Agents;

/// <summary>
/// Background service that manages the ChessAgent's lifecycle:
/// - Connects to SignalR hub
/// - Listens for game events (room creation, turns, messages)
/// - Invokes the ChatClientAgent with appropriate tools via RunAsync
/// </summary>
public class ChessAgentHostedService : BackgroundService, IDisposable
{
    private readonly IGameService _gameService;
    private readonly HashSet<string> _joinedRooms = [];

    private HubConnection? _connection;
    private ChessAgent? _agent;

    public ChessAgentHostedService(IConfiguration configuration, IGameService gameService)
    {
        _gameService = gameService;

        // Build SignalR connection
        _connection = new HubConnectionBuilder()
            .WithUrl(configuration["SignalR:HubUrl"] ?? "http://localhost:5039/gamehub")
            .WithAutomaticReconnect()
            .Build() ?? throw new InvalidOperationException("Failed to build HubConnection.");

        _agent = new ChessAgent(configuration, new ChessAgentTools(_gameService, _connection));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Register SignalR event handlers
        RegisterEventHandlers(stoppingToken);

        // Connect to SignalR hub
        try
        {
            await _connection!.StartAsync(stoppingToken);
            Log.Information("ChessAgent ({AgentName}) connected to SignalR Hub.", ChessAgent.Name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error starting ChessAgent SignalR connection.");
        }

        // Keep alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void RegisterEventHandlers(CancellationToken stoppingToken)
    {
        _connection!.On<Guid, GameType, bool>("RoomCreated", async (roomId, type, isAgainstAi) =>
        {
            if (isAgainstAi)
            {
                Log.Information("AI joining SignalR group for room {RoomId}", roomId);
                await _connection!.InvokeAsync("JoinRoom", roomId.ToString(), ChessAgent.Name, stoppingToken);
                _joinedRooms.Add(roomId.ToString());
            }
        });

        _connection!.On<string, string>("UserJoined", async (roomId, userName) =>
        {
            if (userName != ChessAgent.Name)
            {
                var rid = Guid.Parse(roomId);
                var room = _gameService.GetRoom(rid);
                if (!room.IsPlayingAgainstAi)
                    return;

                var playerSide = room.Players[0].Side;
                var agentPlayingSide = playerSide is Side.First ? Side.Second : Side.First;
                Log.Information("AI joining room {RoomId} to play on {Side}.", rid, agentPlayingSide);

                _agent!.SetPlayingSide(agentPlayingSide);
                _gameService.JoinRoom(rid, ChessAgent.Name, agentPlayingSide);

                // Greet opponent
                await _agent!.InvokeAgent(roomId,
                    $"A new opponent named {userName} has joined the game room {roomId} to play against you. Greet them in character and mock them. " +
                    "Use the SendChatMessage tool to send your greeting.",
                    stoppingToken);

                if (agentPlayingSide == Side.First)
                {
                    await _agent!.PlayYourTurn(roomId, stoppingToken);
                }
            }
        });

        _connection!.On<string, string, string>("ReceiveMessage", async (roomId, userName, message) =>
        {
            if (userName != ChessAgent.Name)
            {
                await _agent!.RespondToChat(roomId, userName, message, stoppingToken);
            }
        });

        _connection!.On<string, string, object>("ReceiveMove", async (roomId, userName, moveData) =>
        {
            if (userName != ChessAgent.Name)
            {
                // Optionally comment on the opponent's move
                // await InvokeAgent(roomId,
                //     $"{userName} just made a move: {moveData}. Optionally comment on it using SendChatMessage.",
                //     stoppingToken);
            }
        });

        _connection!.On<string, string, string>("ReceiveTurnUpdate", async (roomId, userName, nextTurn) =>
        {
            if (userName == ChessAgent.Name)
            {
                Log.Information("It's {AgentName}'s turn in room {RoomId} ({NextTurn})", ChessAgent.Name, roomId, nextTurn);
                await _agent!.PlayYourTurn(roomId, stoppingToken);
            }
        });
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection != null)
        {
            await _connection.StopAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}