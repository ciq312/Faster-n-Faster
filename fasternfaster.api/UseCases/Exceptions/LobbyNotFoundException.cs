using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class LobbyNotFoundException : NotFoundException
{
    public Guid LobbyId { get; }

    public LobbyNotFoundException(Guid lobbyId)
        : base($"Lobby '{lobbyId}' not found")
    {
        LobbyId = lobbyId;
    }
}
