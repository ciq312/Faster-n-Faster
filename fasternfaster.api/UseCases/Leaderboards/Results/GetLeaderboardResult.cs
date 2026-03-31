

namespace FasterNFaster.Api.UseCases.Leaderboards;

public record GetLeaderboardResults(List<LeaderboardResultDTO> Results);

public record LeaderboardResultDTO(Guid Id, string PlayerName, double BestWPM, double BestAccuracy, int Wins, int WordsTyped, int RacesTyped);