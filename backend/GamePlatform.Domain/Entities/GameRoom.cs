using System;
using GamePlatform.Domain.Enums;

namespace GamePlatform.Domain.Entities;

public class GameRoom
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public GameType Type { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
