namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class InvalidInviteCodeException : DomainException
{
    public InvalidInviteCodeException() : base("Invalid invite code.") { }
}