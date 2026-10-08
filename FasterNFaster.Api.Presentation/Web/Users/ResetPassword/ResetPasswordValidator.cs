using FastEndpoints;
using FasterNFaster.Api.Web.Users.Validation;

namespace FasterNFaster.Api.Web.Users.ResetPassword;

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
