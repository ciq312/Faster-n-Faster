namespace FasterNFaster.Api.Domain.Exceptions;

public class NotFoundException(string message) : StatusException(message);
