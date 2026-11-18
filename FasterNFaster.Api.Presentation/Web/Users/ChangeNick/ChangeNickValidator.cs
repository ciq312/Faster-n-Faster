using FastEndpoints;
using FasterNFaster.Api.Presentation.Web.Auth.Validation;

namespace FasterNFaster.Api.Presentation.Web.Users.ChangeNick;

public class ChangeNickValidator : Validator<ChangeNickRequest>
{
    public ChangeNickValidator()
    {
        RuleFor(x => x.Nick).Nick();
    }
}
