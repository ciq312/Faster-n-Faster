using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby;

public record JoinLobbyCommand(Guid PlayerId, Guid LobbyId, string Nick, string Role, string? InviteCode = null) : IRequest;
