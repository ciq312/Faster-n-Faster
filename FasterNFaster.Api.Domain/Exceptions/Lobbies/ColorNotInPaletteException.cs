namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class ColorNotInPaletteException : ConflictException
{
    public ColorNotInPaletteException() : base("Color is not in the palette. Choose another.") { }
}
