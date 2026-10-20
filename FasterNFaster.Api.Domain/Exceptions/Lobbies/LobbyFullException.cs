namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class LobbyFullException : ConflictException
{
    public LobbyFullException() : base("Lobby is full") { }
}
