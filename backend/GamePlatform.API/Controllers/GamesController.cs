using GamePlatform.API.Hubs;
using GamePlatform.Application.Games;
using GamePlatform.Application.Interfaces;
using GamePlatform.Domain.Enums;
using GamePlatform.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace GamePlatform.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GamesController(IGameService gameService, IHubContext<GameHub> hubContext) : ControllerBase
{
    [HttpPost]
    [Route("Create")]
    public IActionResult CreateGameRoom([FromQuery] GameType type, [FromBody] CreateGameDto dto)
    {
        var room = gameService.CreateRoom(type, dto.Name ?? $"{type} Room");
        return Ok(room);
    }

    [HttpGet("Get/{id}")]
    public IActionResult GetGameRoom(Guid id)
    {
        try
        {
            var room = gameService.GetRoom(id);
            return Ok(room);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Game not found.");
        }
    }

    [HttpPost]
    [Route("{id}/Join")]
    public IActionResult JoinGameRoom(Guid id, [FromQuery] string playerName, [FromQuery] Side side)
    {
        try
        {
            var room = gameService.JoinRoom(id, playerName, side);
            return Ok(room);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Game not found.");
        }
    }

    [HttpPost]
    [Route("{id}/Board/Move")]
    public async Task<IActionResult> MakeMove(Guid id, [FromBody] MoveDto dto)
    {
        try
        {
            var move = new Move(new BoardPosition(dto.SourceRow, dto.SourceCol), new BoardPosition(dto.TargetRow, dto.TargetCol));
            var result = gameService.MakeMove(id, dto.PlayerName, move);
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }

            // Broadcast the move via SignalR
            await hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveMove", new
            {
                sourceRow = dto.SourceRow,
                sourceCol = dto.SourceCol,
                targetRow = dto.TargetRow,
                targetCol = dto.TargetCol
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Route("{id}/EndTurn")]
    public async Task<IActionResult> EndTurn(Guid id)
    {
        try
        {
            var result = gameService.EndTurn(id);
            var room = gameService.GetRoom(id);

            // Broadcast turn update via SignalR
            await hubContext.Clients.Group(id.ToString()).SendAsync("ReceiveTurnUpdate", room.CurrentTurn == Side.First ? "first" : "second");

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class CreateGameDto
{
    public string? Name { get; set; }
}

public class MoveDto
{
    public string PlayerName { get; set; } = string.Empty;
    public int SourceRow { get; set; }
    public int SourceCol { get; set; }
    public int TargetRow { get; set; }
    public int TargetCol { get; set; }
}