namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class HostTransferToSelfException() : BadRequestException("You are already the host");
