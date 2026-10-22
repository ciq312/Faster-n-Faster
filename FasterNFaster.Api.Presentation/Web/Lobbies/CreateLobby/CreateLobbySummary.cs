using FasterNFaster.Api.Presentation.Web.Shared;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;

namespace FasterNFaster.Api.Presentation.Web.Lobbies.CreateLobby;

public class CreateLobbySummary : ApiSummary<CreateLobbyEndpoint>
{
    public CreateLobbySummary()
    {
        Summary = "Create a lobby";
        Description = "Creates a lobby hosted by the caller. A private lobby gets an invite code; a public one doesn't.";
        ExampleRequest = new CreateLobbyRequest("Friday race", true);
        Response<CreateLobbyResult>(201, "Lobby created", example: new CreateLobbyResult(Guid.Parse("b7e6d5c4-3a2b-4c1d-9e8f-7a6b5c4d3e2f"), "K7P2QX"));
        ValidationFailed();
        Response<ApiErrorResponse>(401, "Not logged in (`not_authenticated`)");
        Response<ApiErrorResponse>(403, "Not a player or a guest (`forbidden`)");
        Response<ApiErrorResponse>(409, "The caller is already in a lobby");
        RateLimited();
    }
}
