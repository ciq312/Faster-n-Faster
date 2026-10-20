namespace FasterNFaster.Api.Domain.Exceptions;

public class ForbiddenException(string message) : StatusException(message);
