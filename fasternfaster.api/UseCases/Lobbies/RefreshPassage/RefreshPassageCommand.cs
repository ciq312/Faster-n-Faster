namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public record RefreshPassageCommand(Guid CallerId, Guid LobbyId);