namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class HostKickSelfException() : BadRequestException("You can't kick yourself");
