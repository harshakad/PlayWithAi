namespace GamePlatform.Domain.Exceptions;

/// <summary>
/// Exception for domain-level rule violations.
/// </summary>
public class GameDomainException : Exception
{
    public GameDomainException(string message) : base(message) { }
    public GameDomainException(string message, Exception innerException) : base(message, innerException) { }
}
