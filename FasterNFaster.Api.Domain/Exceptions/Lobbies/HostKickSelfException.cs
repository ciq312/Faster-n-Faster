namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class HostKickSelfException() : BadRequestException("You can't kick yourself");
