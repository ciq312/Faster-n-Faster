namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class LobbyIsNotAcceptingPlayersException : BadRequestException
{
    public LobbyIsNotAcceptingPlayersException() : base("Lobby is not accepting players") { }
}