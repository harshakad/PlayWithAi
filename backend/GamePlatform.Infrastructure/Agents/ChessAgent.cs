using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Entities.Chess;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace GamePlatform.Infrastructure.Agents
{
    public class ChessAgent(
        IConfiguration configuration,
        IGameService gameService) : BackgroundService
    {
        private const string Name = "Pepe";
        private string Instructions => $"You are a drunk chessplayer named {Name}{sideDescriptor}. Respond in a humorous and engaging manner, as if you were a drunk chess player. Keep responses concise and entertaining.";
        private Side? playingSide = null;

        private readonly HashSet<string> _joinedRooms = [];

        private HubConnection? _connection;
        private IChatClient? _chatClient;

        private readonly ResiliencePipeline<Move> _retryPipeline = new ResiliencePipelineBuilder<Move>()
            .AddRetry(new RetryStrategyOptions<Move>
            {
                ShouldHandle = new PredicateBuilder<Move>().Handle<Exception>(),
                Delay = TimeSpan.FromSeconds(1),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Constant,
                OnRetry = args =>
                {
                    Log.Warning("Retrying ThinkOfNextMove. Attempt: {AttemptNumber}. Error: {Error}", args.AttemptNumber, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        private string sideDescriptor => playingSide.HasValue ? $", playing as {PlayingSideColour}" : "";

        private string PlayingSideColour => playingSide switch
        {
            Side.First => "white",
            Side.Second => "black",
            _ => "unknown"
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
            var modelId = configuration["Ollama:ModelId"] ?? "gemma4:latest";//"qwen3.5:latest";
            var hubUrl = configuration["SignalR:HubUrl"] ?? "http://localhost:5039/gamehub";

            _chatClient = new OllamaChatClient(new Uri(ollamaUrl), modelId);

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build() ?? throw new InvalidOperationException("Failed to build HubConnection.");

            var greeting = _chatClient.GetResponseAsync("Greet opponent", new ChatOptions
            {
                Temperature = 0.9f,
                Instructions = Instructions
            }, stoppingToken);

            _connection.On<Guid, GameType, bool>("RoomCreated", async (roomId, type, isAgainstAi) =>
            {
                if (isAgainstAi)
                {
                    Log.Information("AI joining SignalR group for room {RoomId}", roomId);
                    await _connection.InvokeAsync("JoinRoom", roomId.ToString(), Name, stoppingToken);
                    _joinedRooms.Add(roomId.ToString());
                }
            });

            _connection.On<string, string>("UserJoined", async (roomId, userName) =>
            {
                if (userName != Name)
                {
                    await ConsiderJoiningGame(Guid.Parse(roomId), stoppingToken);

                    var response = await greeting;
                    await _connection.InvokeAsync("SendMessage", roomId.ToString(), Name, response.Text, stoppingToken);
                }
            });

            _connection.On<string, string, string>("ReceiveMessage", async (roomId, userName, message) =>
            {
                if (userName != Name)
                {
                    await ConsiderReplyingToChat(roomId, message, stoppingToken);
                }
            });

            _connection.On<string, string, object>("ReceiveMove", async (roomId, userName, moveData) =>
            {
                if (userName != Name)
                {
                    await ConsiderCommentingOnMove(roomId, userName, moveData, stoppingToken);
                }
            });

            _connection.On<string, string, string>("ReceiveTurnUpdate", async (roomId, userName, nextTurn) =>
            {
                if (userName == Name)
                {
                    await PlayNextMove(roomId, nextTurn, stoppingToken);
                }
            });

            try
            {
                await _connection.StartAsync(stoppingToken);
                Log.Information("ChessAgent connected to SignalR Hub.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error starting ChessAgent SignalR connection.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ConsiderJoiningGame(Guid roomId, CancellationToken stoppingToken)
        {
            var room = gameService.GetRoom(roomId);
            if (!room.IsPlayingAgainstAi)
                return;

            playingSide = room.Players[0].Side is Side.First ? Side.Second : Side.First;
            Log.Information("AI joining room {RoomId} to play on {Side}.", roomId, playingSide);
            gameService.JoinRoom(roomId, Name, playingSide.Value);
        }

        private async Task PlayNextMove(string roomId, string nextTurn, CancellationToken stoppingToken)
        {
            Log.Information("Turn update in room {RoomId}: it's now {UserName}'s turn ({NextTurn})", roomId, Name, nextTurn);
            var room = gameService.GetRoom(Guid.Parse(roomId));
            var game = room.Game as ChessGame;
            var boardState = game?.Board.ToFen();

            Move? move = null;
            MoveResult? moveResult = null;
            var retryCount = 0;
            var previousMoveError = string.Empty;
            do
            {
                move = await _retryPipeline.ExecuteAsync(token => ThinkOfNextMove(boardState, token, previousMoveError), stoppingToken);

                moveResult = gameService.MakeMove(room.Id, Name, move);

                previousMoveError = moveResult.ErrorMessage;
                retryCount++;
            } while (!moveResult.IsSuccess && retryCount < 3);

            if (!moveResult.IsSuccess)
            {
                Log.Warning("AI attempted to make an invalid move in room {RoomId}: {Move}. Error: {ErrorMessage}", roomId, move, moveResult.ErrorMessage);
            }

            await _connection.InvokeAsync("MakeMove", roomId, Name, new
            {
                sourceRow = move.From.Row,
                sourceCol = move.From.Col,
                targetRow = move.To.Row,
                targetCol = move.To.Col,
                isGameOver = moveResult.IsGameOver,
                gameOverReason = moveResult.GameOverReason
            }, stoppingToken);

            if (!moveResult.IsGameOver)
            {
                gameService.EndTurn(room.Id);
                var nextPlayer = room.Players.FirstOrDefault(p => p.Side == room.CurrentTurn)?.UserName ?? room.CurrentTurn.ToString();
                await _connection.InvokeAsync("EndTurn", roomId, nextPlayer, room.CurrentTurn == Side.First ? "first" : "second", stoppingToken);
            }
        }

        private async ValueTask<Move> ThinkOfNextMove(string? boardState, CancellationToken stoppingToken, string previousMoveError)
        {
            var previousMoveErrorInstruction = string.IsNullOrEmpty(previousMoveError) ? "" : $" The previous move suggestion resulted in an error: {previousMoveError}. Please consider error and avoid suggesting invalid moves.";
            var response = await _chatClient.GetResponseAsync("suggest optimal next move for side " + PlayingSideColour + ", in UCI(Universal Chess Interface) format consisting of 4 characters where file is between a-h and rank is between 1-8", new ChatOptions
            {
                Instructions = $@"
The current board state in FEN notation is: {boardState}.
Respond with your next move in UCI(Universal Chess Interface) consisting of 4 characters only e.g. e2e4.
you should always protect your king and attack opponent's pieces to gain tactical advantage. Consider the current board state and suggest the best move for you, playing as {PlayingSideColour}.
{previousMoveErrorInstruction}"
            }, stoppingToken);

            Log.Information("AI suggests move {Move} for side {Side}", response.Text, PlayingSideColour);
            var move = new Move(uci: response.Text);
            return move;
        }

        private async Task ConsiderReplyingToChat(string roomId, string message, CancellationToken ct)
        {
            await ProcessMessage(roomId, message, ct, new ChatOptions
            {
                Temperature = 0.9f,
                Instructions = Instructions
            });
        }

        private async Task ConsiderCommentingOnMove(string roomId, string userName, object moveData, CancellationToken ct)
        {
            //Log.Information("AI observed a move by {UserName} in room {RoomId}: {MoveData}", userName, roomId, moveData);
            //string observationQuery = $"You are observing a chess game. {userName} made a move: {moveData}. Provide a brief analysis of the move.";
            //await ProcessMessage(roomId, observationQuery, ct);
        }

        private async Task ProcessMessage(string roomId, string chatQuery, CancellationToken ct, ChatOptions chatOptions = null)
        {
            if (_chatClient == null || _connection == null) return;

            try
            {
                var response = await _chatClient.GetResponseAsync(chatQuery, chatOptions, cancellationToken: ct);

                await _connection.InvokeAsync("SendMessage", roomId, Name, response.Text, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting response from Ollama.");
            }
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
}