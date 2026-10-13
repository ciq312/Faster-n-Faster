namespace FasterNFaster.Api.Domain.Exceptions;

public class BadRequestException(string message) : StatusException(message);
