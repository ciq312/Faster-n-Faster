using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.BanForCheat;

public record BanForCheatCommand(Guid UserId, string Reason) : IRequest;
