namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class LobbyIsNotAcceptingPlayersException : BadRequestException
{
    public LobbyIsNotAcceptingPlayersException() : base("Lobby is not accepting players") { }
}
