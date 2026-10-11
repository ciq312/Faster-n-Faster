using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Lobbies.Exceptions;

public class LobbyNotFoundException : NotFoundException
{
    public Guid LobbyId { get; }

    public LobbyNotFoundException(Guid lobbyId)
        : base($"Lobby '{lobbyId}' not found")
    {
        LobbyId = lobbyId;
    }
}
