namespace FasterNFaster.Api.UseCases.Leaderboards;

public record GetLeaderboardCommand(string Criteria, bool IsDescending, int PlayersCount = 30);
