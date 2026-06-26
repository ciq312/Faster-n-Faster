namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class ColorIsAlreadyTakenException : ConflictException
{
    public ColorIsAlreadyTakenException() : base("Color is taken. Choose another.") { }
}