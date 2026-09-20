using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.ChangeColor;

public record ChangeColorCommand(Guid UserId, string Color) : IRequest, ILobbyStateRequest;
