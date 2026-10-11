using FastEndpoints;

namespace FasterNFaster.Api.Presentation.Web.Users.RequestPasswordReset;

public class RequestPasswordResetValidator : Validator<RequestPasswordResetRequest>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email can't be empty")
            .EmailAddress().WithMessage("Only email addresses accepted");
    }
}
