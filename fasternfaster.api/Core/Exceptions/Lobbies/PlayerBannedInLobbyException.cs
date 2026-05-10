namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class PlayerBannedInLobbyException : DomainException
{
    public PlayerBannedInLobbyException() : base("You are banned") { }
}