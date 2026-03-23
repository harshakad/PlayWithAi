using GamePlatform.Domain.Enums;

namespace GamePlatform.Domain.Entities;

/// <summary>
/// Represents a player in a game room. Identity is UserName within the context of a room.
/// </summary>
public class Player
{
    public string UserName { get; }
    public Side Side { get; }

    public Player(string userName, Side side)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Username cannot be empty.", nameof(userName));

        UserName = userName;
        Side = side;
    }

    // Equality by UserName within a room context
    public override bool Equals(object? obj) =>
        obj is Player other && UserName == other.UserName;

    public override int GetHashCode() => UserName.GetHashCode();
}
