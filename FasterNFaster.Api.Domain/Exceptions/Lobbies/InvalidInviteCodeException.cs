namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class InvalidInviteCodeException : NotFoundException
{
    public InvalidInviteCodeException() : base("Invalid invite code.") { }
}