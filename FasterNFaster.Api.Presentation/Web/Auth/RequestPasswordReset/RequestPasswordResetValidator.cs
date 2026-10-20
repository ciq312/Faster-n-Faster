using FastEndpoints;

namespace FasterNFaster.Api.Presentation.Web.Auth.RequestPasswordReset;

public class RequestPasswordResetValidator : Validator<RequestPasswordResetRequest>
{
    public RequestPasswordResetValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email can't be empty")
            .EmailAddress().WithMessage("Only email addresses accepted");
    }
}
