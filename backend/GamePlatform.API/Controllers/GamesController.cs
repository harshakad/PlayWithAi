using System;
using Microsoft.AspNetCore.Mvc;
using GamePlatform.Domain.Enums;
using GamePlatform.Application.Games.Chess;
using GamePlatform.Application.Games.Checkers;
using GamePlatform.Application.Interfaces;

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
        var service = GetService(type);
        var success = service.MakeMove(id, dto.Move);
        if (success) return Ok();
        return BadRequest("Move failed");
    }
}

public class CreateGameDto { public string? Name { get; set; } }
public class MoveDto { public string Move { get; set; } = string.Empty; }
