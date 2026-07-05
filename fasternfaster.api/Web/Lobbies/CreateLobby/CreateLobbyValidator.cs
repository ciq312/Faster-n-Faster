using FastEndpoints;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby;

public class CreateLobbyValidator : Validator<CreateLobbyRequest>
{
    private static readonly string[] ValidGameModes = ["wordcount", "timer"];

    public CreateLobbyValidator()
    {
        RuleFor(x => x.LobbyName)
            .NotEmpty().WithMessage("Lobby name is required.")
            .MaximumLength(30).WithMessage("Lobby name must be 30 characters or fewer.");
    }
}
