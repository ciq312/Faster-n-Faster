namespace FasterNFaster.Api.Core.Exceptions.Lobbies.Races;

public class CheaterDetectedException(string reason)
    : ForbiddenException("Cheating detected")
{
    public string Reason { get; } = reason;
}
