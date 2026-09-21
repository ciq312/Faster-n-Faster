namespace FasterNFaster.Api.UseCases.Interfaces.Users;

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
