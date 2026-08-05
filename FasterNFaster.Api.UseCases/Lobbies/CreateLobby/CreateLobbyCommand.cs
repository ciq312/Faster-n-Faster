using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby;

public record CreateLobbyCommand(string LobbyName, bool IsPrivate, Guid HostId) : IRequest<CreateLobbyResult>;
