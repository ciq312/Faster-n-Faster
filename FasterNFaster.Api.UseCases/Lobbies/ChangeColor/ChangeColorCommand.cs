using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public record ChangeColorCommand(Guid UserId, string Color) : IRequest, ILobbyStateRequest;
