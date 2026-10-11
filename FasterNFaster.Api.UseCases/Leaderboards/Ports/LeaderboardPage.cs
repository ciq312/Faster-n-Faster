namespace FasterNFaster.Api.UseCases.Leaderboards.Ports;

public record LeaderboardPage(IReadOnlyList<LeaderboardEntry> Items, int TotalPlayers);

public record LeaderboardEntry(
    Guid Id,
    string PlayerName,
    float BestWPM,
    float BestAccuracy,
    float AvgWPM,
    float AvgAccuracy,
    int Wins,
    int WordsTyped,
    int RacesTyped);
