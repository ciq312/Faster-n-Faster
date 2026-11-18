using FastEndpoints;
using FasterNFaster.Api.Presentation.Web.Auth.Validation;

namespace FasterNFaster.Api.Presentation.Web.Auth.RegisterAnonymous;

public class RegisterAnonymousValidator : Validator<RegisterAnonymousRequest>
{
    public RegisterAnonymousValidator()
    {
        RuleFor(x => x.Nick).Nick();
    }
}
