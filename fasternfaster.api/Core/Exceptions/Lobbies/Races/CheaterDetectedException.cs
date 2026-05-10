namespace FasterNFaster.Api.Core.Exceptions.Lobbies.Races;

public class CheaterDetectedException(string reason)
    : DomainException($"Cheating detected")
{
    public string Reason { get; } = reason;
}
