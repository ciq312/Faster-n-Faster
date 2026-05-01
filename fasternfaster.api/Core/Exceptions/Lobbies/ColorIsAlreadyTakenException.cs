namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class ColorIsAlreadyTakenException : DomainException
{
    public ColorIsAlreadyTakenException() : base("Color is taken. Choose another.") { }
}