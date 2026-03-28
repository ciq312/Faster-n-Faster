namespace FasterNFaster.Api.UseCases.Exceptions;

public class LobbyNotFoundException : Exception
{
    public Guid LobbyId { get; }

    public LobbyNotFoundException(Guid lobbyId)
        : base($"Lobby '{lobbyId}' not found")
    {
        LobbyId = lobbyId;
    }
}
