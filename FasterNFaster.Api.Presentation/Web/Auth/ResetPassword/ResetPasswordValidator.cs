using FastEndpoints;
using FasterNFaster.Api.Presentation.Web.Auth.Validation;

namespace FasterNFaster.Api.Presentation.Web.Auth.ResetPassword;

public class ResetPasswordValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        RuleFor(x => x.NewPassword)
            .Password();
    }
}
