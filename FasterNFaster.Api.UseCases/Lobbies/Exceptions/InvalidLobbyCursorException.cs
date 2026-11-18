using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Lobbies.Exceptions;

public class InvalidLobbyCursorException() : BadRequestException("The lobby list cursor is invalid")
{
    public override string Code => ErrorCodes.InvalidCursor;
}
