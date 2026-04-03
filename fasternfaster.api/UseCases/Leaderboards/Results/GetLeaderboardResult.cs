

namespace FasterNFaster.Api.UseCases.Leaderboards;

public record GetLeaderboardResults(List<LeaderboardResultDTO> Results);

public record LeaderboardResultDTO(Guid Id, string PlayerName, double BestWPM, double BestAccuracy, double AvgWPM, double AvgAccuracy, int Wins, int WordsTyped, int RacesTyped);