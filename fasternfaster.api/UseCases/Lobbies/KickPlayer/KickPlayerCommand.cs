namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public record KickPlayerCommand(Guid UserId, Guid LobbyId, Guid TargetPlayerId);
