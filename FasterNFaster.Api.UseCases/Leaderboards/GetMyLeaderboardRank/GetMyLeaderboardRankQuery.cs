using FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;
using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards.GetMyLeaderboardRank;

public record GetMyLeaderboardRankQuery(Guid PlayerId, LeaderboardSort Sort, bool Descending) : IRequest<LeaderboardResultDTO>;
