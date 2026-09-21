using FastEndpoints;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby;

public class CreateLobbyValidator : Validator<CreateLobbyRequest>
{
    public CreateLobbyValidator()
    {
        RuleFor(x => x.LobbyName)
            .NotEmpty().WithMessage("Lobby name is required.")
            .MaximumLength(30).WithMessage("Lobby name must be 30 characters or fewer.");
    }
}
