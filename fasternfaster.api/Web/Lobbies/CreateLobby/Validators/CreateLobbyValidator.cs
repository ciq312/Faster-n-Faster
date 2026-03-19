using FastEndpoints;
using FasterNFaster.Api.Web.Lobbies.CreateLobby.Endpoints;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby.Validators;

public class CreateLobbyValidator : Validator<CreateLobbyRequest>
{
    private static readonly string[] ValidGameModes = ["wordcount", "timer"];

    public CreateLobbyValidator()
    {
        RuleFor(x => x.LobbyName)
            .NotEmpty().WithMessage("Lobby name is required.")
            .MaximumLength(30).WithMessage("Lobby name must be 30 characters or fewer.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(20).WithMessage("Display name must be 20 characters or fewer.");

        RuleFor(x => x.GameMode)
            .NotEmpty().WithMessage("Game mode is required.")
            .Must(mode => ValidGameModes.Contains(mode.ToLowerInvariant()))
            .WithMessage("Game mode must be 'wordcount' or 'timer'.");

        RuleFor(x => x.WordCount)
            .GreaterThan(0).WithMessage("Word count must be greater than 0.")
            .When(x => x.GameMode?.ToLowerInvariant() == "wordcount");

        RuleFor(x => x.TimerDurationSeconds)
            .GreaterThan(0).WithMessage("Timer duration must be greater than 0.")
            .When(x => x.GameMode?.ToLowerInvariant() == "timer");
    }
}
