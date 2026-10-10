namespace FasterNFaster.Api.Core.Exceptions.Lobbies;

public class NotHostException(string action) : ForbiddenException($"Only the host can {action}");
