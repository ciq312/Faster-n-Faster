namespace FasterNFaster.Api.UseCases.Lobbies.Disconnect;

public record DisconnectResult(Guid PlayerId, Guid? NewHostId, bool ShouldDeregisterTicks);
