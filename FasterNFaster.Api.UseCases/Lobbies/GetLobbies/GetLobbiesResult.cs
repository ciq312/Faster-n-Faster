namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record GetLobbiesResult(IReadOnlyList<LobbyListItem> Items, string? NextCursor);
