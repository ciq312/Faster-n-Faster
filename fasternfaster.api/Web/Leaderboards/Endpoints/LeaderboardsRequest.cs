namespace FasterNFaster.Api.Web.Leaderboards;

public record GetLeaderboardsRequest(string Criteria, bool IsDescending, int PlayersCount = 30);
