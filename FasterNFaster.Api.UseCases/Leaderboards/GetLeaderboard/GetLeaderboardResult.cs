using FasterNFaster.Api.UseCases.Leaderboards.Ports;

namespace FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;

public record GetLeaderboardResults(
    List<LeaderboardResultDTO> Items,
    int Page,
    int PageSize,
    int TotalPlayers,
    int TotalPages);

public record LeaderboardResultDTO(
    int Rank,
    Guid Id,
    string Nick,
    double BestWPM,
    double BestAccuracy,
    double AvgWPM,
    double AvgAccuracy,
    int Wins,
    int WordsTyped,
    int RacesTyped)
{
    public static LeaderboardResultDTO From(int rank, LeaderboardEntry entry) =>
        new(rank, entry.Id, entry.Nick, entry.BestWPM, entry.BestAccuracy, entry.AvgWPM, entry.AvgAccuracy, entry.Wins, entry.WordsTyped, entry.RacesTyped);
}
