using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Leaderboards.Exceptions;

public class NotOnLeaderboardException() : NotFoundException("You're not on the leaderboard yet")
{
    public override string Code => ErrorCodes.NotOnLeaderboard;
}
