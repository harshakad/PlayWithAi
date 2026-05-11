using GamePlatform.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Serilog;
using System.Collections.Concurrent;

namespace GamePlatform.Infrastructure.Agents;

public sealed class ChessAgent : IDisposable
{
    public const string Name = "Pepe";

    private ChessAgentTools? _tools;

    // Per-room sessions to maintain conversation context
    private readonly ConcurrentDictionary<string, AgentSession> _roomSessions = [];

    //private IList<AITool> _tools = [];

    public ChessAgent(IConfiguration configuration, ChessAgentTools chessAgentTools)
    {
        var ollamaUrl = configuration["Ollama:Url"] ?? "http://localhost:11434";
        var modelId = configuration["Ollama:ModelId"] ?? "gemma4:latest";

        var chatClient = ((IChatClient)new OllamaApiClient(new Uri(ollamaUrl), modelId))
            .AsBuilder()
            .UseFunctionInvocation(null, configure => configure.IncludeDetailedErrors = true)
            .ConfigureOptions(options =>
            {
                //options.AllowMultipleToolCalls = true;
                //options.Temperature = 0.9f;
            })
            .Build();

        _tools = chessAgentTools;

        Agent = chatClient.AsAIAgent(instructions: Instructions,
                                    tools: [AIFunctionFactory.Create(chessAgentTools.GetStockfishMove),
                                            AIFunctionFactory.Create(chessAgentTools.PlayStockfishMove),
                                            AIFunctionFactory.Create(chessAgentTools.SendChatMessage),
                                            AIFunctionFactory.Create(chessAgentTools.GetBoardState)]);
    }

    /// <summary>
    /// The underlying ChatClientAgent instance.
    /// Used by the hosted service to invoke RunAsync with tools.
    /// </summary>
    public AIAgent Agent { get; init; }

    /// <summary>
    /// Invokes the ChatClientAgent with the given prompt and all available tools.
    /// The agent will autonomously decide which tools to call based on the prompt.
    /// </summary>
    public async Task<string> InvokeAgent(string roomId, string prompt, CancellationToken ct)
    {
        if (Agent is null) return string.Empty;

        try
        {
            // Get or create session for this room (maintains conversation history)
            if (!_roomSessions.TryGetValue(roomId, out var session))
            {
                session = await Agent.CreateSessionAsync(ct);
                _roomSessions[roomId] = session;
            }

            var response = await Agent.RunAsync(
                prompt,
                //session: session,
                //options: new ChatClientAgentRunOptions(chatOptions),
                cancellationToken: ct);

            Log.Information("Agent response for room {RoomId}: {Response}", roomId, response.Text);

            return response.Text;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error invoking ChessAgent for room {RoomId}", roomId);
            return string.Empty;
        }
    }

    public void SetPlayingSide(Side side)
    {
        _playingSide = side;
    }

    public async Task RespondToChat(string roomId, string userName, string message, CancellationToken stoppingToken)
    {
        await InvokeAgent(roomId,
            $"{userName} in room {roomId} says: \"{message}\". Use the SendChatMessage tool to send your response.",
            stoppingToken);
    }

    public async Task PlayYourTurn(string roomId, CancellationToken stoppingToken)
    {
        var fen = _tools!.GetBoardState(roomId);

        await InvokeAgent(roomId,
            $"In room {roomId}, the current board state FEN is: '{fen}'. " +
            $"Use GetStockfishMove with that FEN to get the StockfishMove." +
            $"Then use PlayStockfishMove to play that specific StockfishMove.",
            stoppingToken);
    }

    public void Dispose()
    {
        _tools?.Dispose();
    }

    private Side? _playingSide;

    private string SideDescriptor => _playingSide.HasValue ? $", playing as {PlayingSideColour}" : "";

    private string PlayingSideColour => _playingSide switch
    {
        Side.First => "white",
        Side.Second => "black",
        _ => "unknown"
    };

    private string Instructions => $"""
        You are a drunk chess player named {Name}{SideDescriptor}.
        Respond in a humorous and engaging manner, as if you were a drunk chess player.
        Keep responses concise and entertaining.
        When chatting, always stay in character as a drunk chess player named {Name}.
        """;
}