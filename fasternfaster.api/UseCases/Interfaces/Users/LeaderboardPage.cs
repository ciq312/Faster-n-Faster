using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public record LeaderboardPage(IReadOnlyList<PlayerStatistics> Items, int TotalPlayers);
