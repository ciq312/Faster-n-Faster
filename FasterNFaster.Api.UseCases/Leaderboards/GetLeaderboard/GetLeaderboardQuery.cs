using FasterNFaster.Api.UseCases.Leaderboards.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards.GetLeaderboard;

public record GetLeaderboardQuery(LeaderboardSort Sort, bool Descending, int Page, int PageSize) : IRequest<GetLeaderboardResults>;
