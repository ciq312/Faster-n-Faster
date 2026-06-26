namespace FasterNFaster.Api.Core.Exceptions;

public class DomainException(string message) : StatusException(message) { }