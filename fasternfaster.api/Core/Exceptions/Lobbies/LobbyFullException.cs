namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class LobbyFullException : DomainException
{
    public LobbyFullException() : base("Lobby is full") { }
}