using MediatR;

namespace FasterNFaster.Api.UseCases.Leaderboards;

public record GetLeaderboardCommand(LeaderboardSort Sort, bool Descending, int Page, int PageSize) : IRequest<GetLeaderboardResults>;
