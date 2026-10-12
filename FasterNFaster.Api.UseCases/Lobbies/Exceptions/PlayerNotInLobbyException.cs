using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Lobbies.Exceptions;

public class PlayerNotInLobbyException(Guid playerId) : NotFoundException($"Player {playerId} is not in a lobby");
