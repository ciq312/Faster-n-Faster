using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.StartRace;

public record StartRaceCommand(Guid UserId) : IRequest<Guid>, ILobbyStateRequest;
