using FasterNFaster.Api.UseCases.Leaderboards;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public record LeaderboardPage(IReadOnlyList<LeaderboardPlayerReadModel> Items, int TotalPlayers);
