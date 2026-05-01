namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class LobbyIsNotAcceptingPlayersException : DomainException
{
    public LobbyIsNotAcceptingPlayersException() : base("Lobby is not accepting players") { }
}