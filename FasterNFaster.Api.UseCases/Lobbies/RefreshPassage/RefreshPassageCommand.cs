using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public record RefreshPassageCommand(Guid CallerId) : IRequest, ILobbyStateRequest;
