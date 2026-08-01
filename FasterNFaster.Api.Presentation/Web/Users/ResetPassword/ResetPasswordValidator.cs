using FastEndpoints;

namespace FasterNFaster.Api.Web.Users.ResetPassword;

public class ResetPasswordValidator : Validator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required");

        // Same rules as registration — if they ever drift out of sync,
        // factor into a shared rule-set.
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Password can't be empty")
            .MinimumLength(4).WithMessage("Password min length is 4")
            .MaximumLength(30).WithMessage("Password max length is 30");
    }
}
