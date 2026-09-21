using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.KickPlayer;

public record KickPlayerCommand(Guid UserId, Guid TargetPlayerId) : IRequest<KickPlayerResult>;
