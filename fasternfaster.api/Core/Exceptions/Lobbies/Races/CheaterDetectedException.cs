namespace FasterNFaster.Api.Core.Exceptions.Lobbies.Races;

public class CheaterDetectedException(string reason)
    : DomainException($"Cheater detected: {reason}")
{
    public string Reason { get; } = reason;
}
