namespace FasterNFaster.Api.UseCases.Leaderboards;

/// <summary>Whitelisted sortable leaderboard columns. Binding to this enum rejects arbitrary property names.</summary>
public enum LeaderboardSort
{
    BestWpm,
    AvgWpm,
    BestAccuracy,
    AvgAccuracy,
    Wins,
    WordsTyped,
    RacesTyped
}
