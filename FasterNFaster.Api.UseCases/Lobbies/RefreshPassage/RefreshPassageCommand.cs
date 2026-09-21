using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public record RefreshPassageCommand(Guid CallerId) : IRequest, ILobbyStateRequest;
