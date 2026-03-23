using GamePlatform.Domain.Enums;

namespace GamePlatform.Domain.ValueObjects;

/// <summary>
/// Represents a game piece on the board. Immutable value object.
/// </summary>
public sealed record Piece(PieceColor Color, PieceType Type);
