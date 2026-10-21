using FasterNFaster.Api.Presentation.Web.Shared;

namespace FasterNFaster.Api.Presentation.Web.Users.GetUserProfile;

public class GetUserProfileSummary : ApiSummary<GetUserProfileEndpoint>
{
    public GetUserProfileSummary()
    {
        Summary = "Get the current player's statistics";
        Response<GetUserProfileResult>(200, "The statistics", example: new GetUserProfileResult(
            new UserStatisticsDTO("player1", 14, 42, 112.4f, 95.7f, 98.1f, 96.3f, 5210)));
        Response<ApiErrorResponse>(401, "Not logged in; the body is empty when the access token is missing or invalid");
        Response<ApiErrorResponse>(404, "The player has no statistics yet");
    }
}
