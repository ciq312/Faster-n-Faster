namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class InvalidInviteCodeException : NotFoundException
{
    public InvalidInviteCodeException() : base("Invalid invite code.") { }

    public override string Code => ErrorCodes.InvalidInviteCode;
}
