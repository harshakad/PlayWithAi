using System;
using GamePlatform.Domain.Entities;

namespace GamePlatform.Application.Interfaces;

public interface IGameService
{
    GameRoom CreateGame(string name);
    GameRoom GetGame(Guid id);
    bool MakeMove(Guid id, string move);
}
