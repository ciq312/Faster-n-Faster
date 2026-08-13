namespace FasterNFaster.Api.UseCases.Leaderboards;

public sealed record LeaderboardPlayerReadModel(
    Guid Id,
    string PlayerName,
    float BestWPM,
    float BestAccuracy,
    float AvgWPM,
    float AvgAccuracy,
    int Wins,
    int WordsTyped,
    int RacesTyped);
