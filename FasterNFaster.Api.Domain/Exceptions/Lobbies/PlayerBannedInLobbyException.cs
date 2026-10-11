namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class PlayerBannedInLobbyException : ForbiddenException
{
    public PlayerBannedInLobbyException() : base("You are banned") { }
}
