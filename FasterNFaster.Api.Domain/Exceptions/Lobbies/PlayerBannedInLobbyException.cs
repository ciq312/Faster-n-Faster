namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class PlayerBannedInLobbyException : ForbiddenException
{
    public PlayerBannedInLobbyException() : base("You are banned") { }
}