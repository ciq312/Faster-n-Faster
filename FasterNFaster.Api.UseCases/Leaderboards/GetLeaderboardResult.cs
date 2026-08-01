namespace FasterNFaster.Api.UseCases.Leaderboards;

public record GetLeaderboardResults(
    List<LeaderboardResultDTO> Items,
    int Page,
    int PageSize,
    int TotalPlayers,
    int TotalPages);

public record LeaderboardResultDTO(
    int Rank,
    Guid Id,
    string PlayerName,
    double BestWPM,
    double BestAccuracy,
    double AvgWPM,
    double AvgAccuracy,
    int Wins,
    int WordsTyped,
    int RacesTyped);
