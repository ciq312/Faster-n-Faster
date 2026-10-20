namespace FasterNFaster.Api.Domain.Exceptions.Lobbies;

public class HostTransferToSelfException() : BadRequestException("You are already the host");
