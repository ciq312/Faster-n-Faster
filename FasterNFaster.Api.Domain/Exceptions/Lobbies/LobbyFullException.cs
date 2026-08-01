namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class LobbyFullException : ConflictException
{
    public LobbyFullException() : base("Lobby is full") { }
}