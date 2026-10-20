namespace FasterNFaster.Api.Domain.Exceptions;

public class UnauthorizedException(string message) : StatusException(message);
