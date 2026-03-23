using GamePlatform.Domain.Enums;
using GamePlatform.Domain.Aggregates;
using GamePlatform.Domain.ValueObjects;
using GamePlatform.Application.Games.Chess;
using GamePlatform.Application.Games.Checkers;
using GamePlatform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GamePlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController : ControllerBase
{
    private readonly IChessGameService _chessService;
    private readonly ICheckersGameService _checkersService;

    public GamesController(IChessGameService chessService, ICheckersGameService checkersService)
    {
        _chessService = chessService;
        _checkersService = checkersService;
    }

    private IGameService GetService(GameType type)
    {
        return type switch
        {
            GameType.Chess => _chessService,
            GameType.Checkers => _checkersService,
            _ => throw new ArgumentException("Invalid game type")
        };
    }

    [HttpPost]
    public IActionResult CreateGame([FromQuery] GameType type, [FromBody] CreateGameDto dto)
    {
        var service = GetService(type);
        var game = service.CreateGame(dto.Name ?? "New Game");
        return Ok(game);
    }

    [HttpGet("{id}")]
    public IActionResult GetGame(Guid id, [FromQuery] GameType type)
    {
        try
        {
            var service = GetService(type);
            var game = service.GetGame(id);
            return Ok(game);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Game not found.");
        }
    }

    [HttpPost("{id}/move")]
    public IActionResult MakeMove(Guid id, [FromQuery] GameType type, [FromBody] MoveDto dto)
    {
        try
        {
            var service = GetService(type);
            var move = new Move(new BoardPosition(dto.SourceRow, dto.SourceCol), new BoardPosition(dto.TargetRow, dto.TargetCol));
            var result = service.MakeMove(id, dto.PlayerName, move);
            
            if (result.IsSuccess) return Ok(result);
            return BadRequest(result.ErrorMessage);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class CreateGameDto { public string? Name { get; set; } }
public class MoveDto 
{ 
    public string PlayerName { get; set; } = string.Empty;
    public int SourceRow { get; set; }
    public int SourceCol { get; set; }
    public int TargetRow { get; set; }
    public int TargetCol { get; set; }
}
