namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public record DisconnectResult(Guid PlayerId, bool ShouldDeregisterTicks);
