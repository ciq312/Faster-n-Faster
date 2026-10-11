using FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;
using MediatR;

namespace FasterNFaster.Api.UseCases.Races.StartRace;

public record StartRaceCommand(Guid UserId) : IRequest<Guid>, ILobbyStateRequest;
